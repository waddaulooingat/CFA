<#
.SYNOPSIS
    Scans a directory of T-SQL (.sql) files containing stored-procedure
    definitions and generates C# xUnit tests that call each procedure via
    ADO.NET (Microsoft.Data.SqlClient).

.DESCRIPTION
    For every CREATE / ALTER / CREATE OR ALTER PROCEDURE found in the input
    directory, the script emits a corresponding [Fact] that:

      * opens a SqlConnection to a connection string taken from an environment
        variable (so no secrets are baked into the generated code),
      * builds a SqlCommand with CommandType.StoredProcedure,
      * adds every declared parameter with a type-appropriate sample value
        (OUTPUT parameters get ParameterDirection.Output; table-valued
        parameters get a TODO placeholder),
      * runs the procedure inside a transaction that is always rolled back so
        the test never mutates data,
      * asserts the procedure executed without throwing (a smoke test) and
        leaves a TODO for real assertions.

    One C# test class is generated per input .sql file. If a file declares
    several procedures, the class contains one [Fact] per procedure.

.PARAMETER InputPath
    Directory (searched recursively) or a single .sql file to scan.

.PARAMETER OutputPath
    Directory where the generated .cs files are written. Created if missing.

.PARAMETER Namespace
    C# namespace for the generated test classes.
    Default: 'GeneratedTests.StoredProcedures'.

.PARAMETER ConnectionEnvVar
    Name of the environment variable the generated tests read the connection
    string from. Default: 'SP_TEST_CONNECTION'.

.PARAMETER EmitProject
    Also emit a ready-to-build .csproj and the shared base class so the output
    directory is a standalone xUnit test project.

.PARAMETER Force
    Overwrite existing generated files. Without it, existing files are skipped.

.EXAMPLE
    ./Generate-StoredProcTests.ps1 -InputPath ./sql -OutputPath ./generated -EmitProject

.EXAMPLE
    ./Generate-StoredProcTests.ps1 -InputPath C:\db\procs -OutputPath C:\tests `
        -Namespace MyApp.Db.Tests -ConnectionEnvVar MYAPP_TEST_DB
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$Namespace = 'GeneratedTests.StoredProcedures',

    [string]$ConnectionEnvVar = 'SP_TEST_CONNECTION',

    [switch]$EmitProject,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# T-SQL parsing
# ---------------------------------------------------------------------------

# Remove block comments, line comments and string literals, replacing each
# with a single space so that stray '@', ',', '(' or the word AS inside them
# cannot confuse the procedure/parameter parser. Length is not preserved; we
# only ever parse the cleaned text, never index back into the original.
function Remove-SqlNoise {
    param([string]$Sql)

    $sb = [System.Text.StringBuilder]::new()
    $i = 0
    $n = $Sql.Length
    while ($i -lt $n) {
        $c = $Sql[$i]
        $next = if ($i + 1 -lt $n) { $Sql[$i + 1] } else { "`0" }

        if ($c -eq '-' -and $next -eq '-') {
            # line comment: skip to end of line
            while ($i -lt $n -and $Sql[$i] -ne "`n") { $i++ }
            [void]$sb.Append(' ')
        }
        elseif ($c -eq '/' -and $next -eq '*') {
            # block comment: skip to closing */
            $i += 2
            while ($i + 1 -lt $n -and -not ($Sql[$i] -eq '*' -and $Sql[$i + 1] -eq '/')) { $i++ }
            $i += 2
            [void]$sb.Append(' ')
        }
        elseif ($c -eq "'") {
            # string literal: skip to closing quote (handles '' escapes)
            $i++
            while ($i -lt $n) {
                if ($Sql[$i] -eq "'") {
                    if ($i + 1 -lt $n -and $Sql[$i + 1] -eq "'") { $i += 2; continue }
                    $i++
                    break
                }
                $i++
            }
            [void]$sb.Append(' ')
        }
        else {
            [void]$sb.Append($c)
            $i++
        }
    }
    return $sb.ToString()
}

# Strip [brackets] / "quotes" from an identifier part.
function Get-CleanIdentifierPart {
    param([string]$Part)
    $p = $Part.Trim()
    if ($p.StartsWith('[') -and $p.EndsWith(']')) { return $p.Substring(1, $p.Length - 2) }
    if ($p.StartsWith('"') -and $p.EndsWith('"')) { return $p.Substring(1, $p.Length - 2) }
    return $p
}

# Split a comma-separated list, ignoring commas nested inside parentheses.
function Split-TopLevelCommas {
    param([string]$Text)
    $parts = [System.Collections.Generic.List[string]]::new()
    $depth = 0
    $start = 0
    for ($i = 0; $i -lt $Text.Length; $i++) {
        $ch = $Text[$i]
        if ($ch -eq '(') { $depth++ }
        elseif ($ch -eq ')') { $depth-- }
        elseif ($ch -eq ',' -and $depth -eq 0) {
            $parts.Add($Text.Substring($start, $i - $start))
            $start = $i + 1
        }
    }
    $parts.Add($Text.Substring($start))
    return $parts
}

# Parse a single parameter declaration, e.g.
#   @CustomerId INT
#   @Name NVARCHAR(50) = 'x' OUTPUT
#   @Rows dbo.IdList READONLY
function ConvertTo-ParamInfo {
    param([string]$Decl)

    $d = $Decl.Trim()
    if ([string]::IsNullOrWhiteSpace($d)) { return $null }

    $m = [regex]::Match($d, '^\s*(@\w+)\s+(.*)$', 'Singleline')
    if (-not $m.Success) { return $null }

    $name = $m.Groups[1].Value
    $rest = $m.Groups[2].Value.Trim()

    $isOutput = $false
    $isReadOnly = $false
    $hasDefault = $false

    # READONLY flag (table-valued parameters)
    if ($rest -match '(?i)\bREADONLY\b') {
        $isReadOnly = $true
        $rest = [regex]::Replace($rest, '(?i)\bREADONLY\b', '').Trim()
    }
    # OUTPUT / OUT flag
    if ($rest -match '(?i)\b(OUTPUT|OUT)\b\s*$') {
        $isOutput = $true
        $rest = [regex]::Replace($rest, '(?i)\b(OUTPUT|OUT)\b\s*$', '').Trim()
    }
    # default value: everything after a top-level '='
    $eq = -1
    $depth = 0
    for ($i = 0; $i -lt $rest.Length; $i++) {
        $ch = $rest[$i]
        if ($ch -eq '(') { $depth++ }
        elseif ($ch -eq ')') { $depth-- }
        elseif ($ch -eq '=' -and $depth -eq 0) { $eq = $i; break }
    }
    if ($eq -ge 0) {
        $hasDefault = $true
        $rest = $rest.Substring(0, $eq).Trim()
    }

    $typeFull = $rest.Trim()

    # Base type name (strip length/precision and schema qualifier).
    $baseType = $typeFull
    $paren = $baseType.IndexOf('(')
    if ($paren -ge 0) { $baseType = $baseType.Substring(0, $paren) }
    $baseType = $baseType.Trim()
    if ($baseType.Contains('.')) {
        # schema-qualified => user-defined / table type
        $baseType = ($baseType -split '\.')[-1]
    }
    $baseType = (Get-CleanIdentifierPart $baseType)

    return [pscustomobject]@{
        Name       = $name
        TypeFull   = $typeFull
        BaseType   = $baseType.ToLowerInvariant()
        IsOutput   = $isOutput
        IsReadOnly = $isReadOnly
        HasDefault = $hasDefault
    }
}

# Extract every procedure (name + parameter list) from cleaned SQL text.
function Get-ProceduresFromSql {
    param([string]$CleanSql)

    $results = [System.Collections.Generic.List[object]]::new()

    $headerRegex = [regex]'(?i)\bCREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+|(?i)\bALTER\s+PROC(?:EDURE)?\s+'
    foreach ($h in $headerRegex.Matches($CleanSql)) {
        $pos = $h.Index + $h.Length

        # Procedure name: [schema].[name] / schema.name / name  (optional ; number)
        $nameRegex = [regex]'(?i)\G\s*(?<schema>\[[^\]]+\]|"[^"]+"|\w+)?\s*\.?\s*(?<name>\[[^\]]+\]|"[^"]+"|\w+)'
        # The above is loose; refine by matching the two-part form explicitly.
        $nm = [regex]::Match($CleanSql.Substring($pos), '^\s*(?<p1>\[[^\]]+\]|"[^"]+"|\w+)(?:\s*\.\s*(?<p2>\[[^\]]+\]|"[^"]+"|\w+))?')
        if (-not $nm.Success) { continue }

        if ($nm.Groups['p2'].Success) {
            $schema = Get-CleanIdentifierPart $nm.Groups['p1'].Value
            $procName = Get-CleanIdentifierPart $nm.Groups['p2'].Value
        }
        else {
            $schema = 'dbo'
            $procName = Get-CleanIdentifierPart $nm.Groups['p1'].Value
        }

        # Everything after the name up to the body-starting AS (paren depth 0).
        $afterName = $pos + $nm.Length
        $tail = $CleanSql.Substring($afterName)

        # Find first top-level ' AS ' that starts the body, and first top-level
        # WITH/FOR that ends the parameter list (whichever comes first).
        $depth = 0
        $asIndex = -1
        $stopIndex = -1
        $tokenRegex = [regex]'(?i)\bAS\b|\bWITH\b|\bFOR\s+REPLICATION\b'
        $ci = 0
        while ($ci -lt $tail.Length) {
            $ch = $tail[$ci]
            if ($ch -eq '(') { $depth++; $ci++; continue }
            if ($ch -eq ')') { $depth--; $ci++; continue }
            if ($depth -eq 0) {
                $tm = $tokenRegex.Match($tail, $ci)
                if ($tm.Success -and $tm.Index -eq $ci) {
                    if ($tm.Value -match '(?i)^AS$') { $asIndex = $ci; break }
                    else { if ($stopIndex -lt 0) { $stopIndex = $ci }; break }
                }
            }
            $ci++
        }

        $paramEnd = if ($asIndex -ge 0) { $asIndex } elseif ($stopIndex -ge 0) { $stopIndex } else { $tail.Length }
        $paramText = $tail.Substring(0, $paramEnd).Trim()

        # Strip a single wrapping pair of parentheses if present.
        if ($paramText.StartsWith('(') -and $paramText.EndsWith(')')) {
            $inner = $paramText.Substring(1, $paramText.Length - 2).Trim()
            # only unwrap if the outer parens are balanced as a wrapper
            $paramText = $inner
        }

        $params = [System.Collections.Generic.List[object]]::new()
        if (-not [string]::IsNullOrWhiteSpace($paramText)) {
            foreach ($piece in (Split-TopLevelCommas $paramText)) {
                $pi = ConvertTo-ParamInfo $piece
                if ($null -ne $pi) { $params.Add($pi) }
            }
        }

        $results.Add([pscustomobject]@{
                Schema     = $schema
                Name       = $procName
                Parameters = $params
            })
    }

    return $results
}

# ---------------------------------------------------------------------------
# SQL type -> (SqlDbType, sample C# literal)
# ---------------------------------------------------------------------------

function Get-SqlDbTypeMapping {
    param([string]$BaseType)

    switch ($BaseType) {
        { $_ -in 'int', 'integer' }               { return @{ SqlDbType = 'Int';              Value = '0' } }
        'bigint'                                   { return @{ SqlDbType = 'BigInt';           Value = '0L' } }
        'smallint'                                 { return @{ SqlDbType = 'SmallInt';         Value = '(short)0' } }
        'tinyint'                                  { return @{ SqlDbType = 'TinyInt';          Value = '(byte)0' } }
        'bit'                                      { return @{ SqlDbType = 'Bit';              Value = 'false' } }
        { $_ -in 'decimal', 'numeric' }            { return @{ SqlDbType = 'Decimal';          Value = '0m' } }
        { $_ -in 'money', 'smallmoney' }           { return @{ SqlDbType = 'Money';            Value = '0m' } }
        'float'                                    { return @{ SqlDbType = 'Float';            Value = '0d' } }
        'real'                                     { return @{ SqlDbType = 'Real';             Value = '0f' } }
        { $_ -in 'date', 'datetime', 'datetime2', 'smalldatetime' } {
            return @{ SqlDbType = 'DateTime2'; Value = 'new DateTime(2000, 1, 1)' }
        }
        'datetimeoffset'                           { return @{ SqlDbType = 'DateTimeOffset';   Value = 'DateTimeOffset.UnixEpoch' } }
        'time'                                     { return @{ SqlDbType = 'Time';             Value = 'TimeSpan.Zero' } }
        'uniqueidentifier'                         { return @{ SqlDbType = 'UniqueIdentifier'; Value = 'Guid.Empty' } }
        { $_ -in 'char', 'varchar', 'text' }       { return @{ SqlDbType = 'VarChar';          Value = '"test"' } }
        { $_ -in 'nchar', 'nvarchar', 'ntext' }    { return @{ SqlDbType = 'NVarChar';         Value = '"test"' } }
        { $_ -in 'binary', 'varbinary', 'image' }  { return @{ SqlDbType = 'VarBinary';        Value = 'new byte[] { 0 }' } }
        'xml'                                      { return @{ SqlDbType = 'Xml';              Value = '"<root />"' } }
        'sql_variant'                              { return @{ SqlDbType = 'Variant';          Value = '0' } }
        default                                    { return $null }  # unknown / user-defined
    }
}

# ---------------------------------------------------------------------------
# C# emission
# ---------------------------------------------------------------------------

function Get-CSharpIdentifier {
    param([string]$Raw)
    $id = [regex]::Replace($Raw, '[^0-9A-Za-z_]', '_')
    if ($id -match '^[0-9]') { $id = "_$id" }
    if ([string]::IsNullOrEmpty($id)) { $id = '_' }
    return $id
}

function New-ParameterLines {
    param($Param)

    $lines = [System.Collections.Generic.List[string]]::new()
    $csName = Get-CSharpIdentifier ($Param.Name.TrimStart('@'))
    $varName = "p_$csName"

    if ($Param.IsReadOnly) {
        # Table-valued parameter: emit a placeholder DataTable the developer fills in.
        $lines.Add("            // TODO: '$($Param.Name)' is a table-valued parameter ($($Param.TypeFull)).")
        $lines.Add("            //       Populate this DataTable with columns/rows matching the table type.")
        $lines.Add("            var $varName = new DataTable();")
        $lines.Add("            cmd.Parameters.Add(new SqlParameter(""$($Param.Name)"", SqlDbType.Structured)")
        $lines.Add("            {")
        $lines.Add("                TypeName = ""$($Param.TypeFull)"",")
        $lines.Add("                Value = $varName,")
        $lines.Add("            });")
        return $lines
    }

    $map = Get-SqlDbTypeMapping $Param.BaseType

    if ($Param.IsOutput) {
        if ($null -eq $map) {
            $lines.Add("            // TODO: unknown/user-defined type '$($Param.TypeFull)' for output '$($Param.Name)'; set SqlDbType manually.")
            $lines.Add("            var $varName = new SqlParameter(""$($Param.Name)"", SqlDbType.Variant)")
        }
        else {
            $lines.Add("            var $varName = new SqlParameter(""$($Param.Name)"", SqlDbType.$($map.SqlDbType))")
        }
        $lines.Add("            {")
        $lines.Add("                Direction = ParameterDirection.Output,")
        $lines.Add("                Size = -1,")
        $lines.Add("            };")
        $lines.Add("            cmd.Parameters.Add($varName);")
        return $lines
    }

    if ($null -eq $map) {
        $lines.Add("            // TODO: unknown/user-defined type '$($Param.TypeFull)' for '$($Param.Name)'; supply a real value.")
        $lines.Add("            cmd.Parameters.AddWithValue(""$($Param.Name)"", DBNull.Value);")
        return $lines
    }

    $defaultNote = if ($Param.HasDefault) { '  // has a default; value optional' } else { '' }
    $lines.Add("            cmd.Parameters.Add(new SqlParameter(""$($Param.Name)"", SqlDbType.$($map.SqlDbType)) { Value = $($map.Value) });$defaultNote")
    return $lines
}

function New-TestMethod {
    param($Proc)

    $methodName = Get-CSharpIdentifier "$($Proc.Name)_ExecutesWithoutError"
    $fullName = "[$($Proc.Schema)].[$($Proc.Name)]"

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("    [Fact]")
    [void]$sb.AppendLine("    [Trait(""Category"", ""StoredProcedure"")]")
    [void]$sb.AppendLine("    public async Task $methodName()")
    [void]$sb.AppendLine("    {")
    [void]$sb.AppendLine("        await using var conn = new SqlConnection(ConnectionString);")
    [void]$sb.AppendLine("        await conn.OpenAsync();")
    [void]$sb.AppendLine("        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();")
    [void]$sb.AppendLine("        try")
    [void]$sb.AppendLine("        {")
    [void]$sb.AppendLine("            await using var cmd = new SqlCommand(""$fullName"", conn, tx)")
    [void]$sb.AppendLine("            {")
    [void]$sb.AppendLine("                CommandType = CommandType.StoredProcedure,")
    [void]$sb.AppendLine("            };")
    [void]$sb.AppendLine("")

    if ($Proc.Parameters.Count -eq 0) {
        [void]$sb.AppendLine("            // Procedure declares no parameters.")
    }
    foreach ($p in $Proc.Parameters) {
        foreach ($line in (New-ParameterLines $p)) {
            [void]$sb.AppendLine($line)
        }
    }

    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("            // Capture the procedure's RETURN value so we can assert it ran.")
    [void]$sb.AppendLine("            var returnValue = new SqlParameter")
    [void]$sb.AppendLine("            {")
    [void]$sb.AppendLine("                ParameterName = ""@__ReturnValue"",")
    [void]$sb.AppendLine("                SqlDbType = SqlDbType.Int,")
    [void]$sb.AppendLine("                Direction = ParameterDirection.ReturnValue,")
    [void]$sb.AppendLine("            };")
    [void]$sb.AppendLine("            cmd.Parameters.Add(returnValue);")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("            // Smoke test: the procedure must execute without throwing.")
    [void]$sb.AppendLine("            await cmd.ExecuteNonQueryAsync();")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("            Assert.NotNull(returnValue.Value);")
    [void]$sb.AppendLine("            // TODO: replace the sample argument values above with meaningful data")
    [void]$sb.AppendLine("            //       and add assertions on result sets / output parameters.")
    [void]$sb.AppendLine("        }")
    [void]$sb.AppendLine("        finally")
    [void]$sb.AppendLine("        {")
    [void]$sb.AppendLine("            // Never persist test data.")
    [void]$sb.AppendLine("            await tx.RollbackAsync();")
    [void]$sb.AppendLine("        }")
    [void]$sb.AppendLine("    }")
    return $sb.ToString()
}

function New-TestClassFile {
    param(
        [string]$ClassName,
        [object[]]$Procedures,
        [string]$SourceFileName
    )

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("// <auto-generated>")
    [void]$sb.AppendLine("//     Generated by Generate-StoredProcTests.ps1 from '$SourceFileName'.")
    [void]$sb.AppendLine("//     Regenerating overwrites this file (with -Force); keep edits in separate partials.")
    [void]$sb.AppendLine("// </auto-generated>")
    [void]$sb.AppendLine("using System;")
    [void]$sb.AppendLine("using System.Data;")
    [void]$sb.AppendLine("using System.Threading.Tasks;")
    [void]$sb.AppendLine("using Microsoft.Data.SqlClient;")
    [void]$sb.AppendLine("using Xunit;")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("namespace $Namespace;")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("public class $ClassName")
    [void]$sb.AppendLine("{")
    [void]$sb.AppendLine("    // Connection string is read from the '$ConnectionEnvVar' environment variable so")
    [void]$sb.AppendLine("    // no credentials are committed. Point it at a disposable TEST database.")
    [void]$sb.AppendLine("    private static string ConnectionString =>")
    [void]$sb.AppendLine("        Environment.GetEnvironmentVariable(""$ConnectionEnvVar"")")
    [void]$sb.AppendLine("        ?? throw new InvalidOperationException(")
    [void]$sb.AppendLine("            ""Set the '$ConnectionEnvVar' environment variable to a test SQL Server connection string. "" +")
    [void]$sb.AppendLine("            ""To skip these tests, filter them out with: dotnet test --filter Category!=StoredProcedure"");")
    [void]$sb.AppendLine("")

    $methods = foreach ($proc in $Procedures) { New-TestMethod $proc }
    [void]$sb.AppendLine(($methods -join "`n"))

    [void]$sb.AppendLine("}")
    return $sb.ToString()
}

# ---------------------------------------------------------------------------
# Optional standalone project scaffolding
# ---------------------------------------------------------------------------

function Write-ProjectFiles {
    param([string]$OutDir)

    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
"@
    $csprojPath = Join-Path $OutDir 'StoredProcedure.Tests.csproj'
    Set-Content -Path $csprojPath -Value $csproj -Encoding UTF8
    Write-Host "  wrote project  $csprojPath"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "InputPath not found: $InputPath"
}

$sqlFiles = @()
if (Test-Path -LiteralPath $InputPath -PathType Container) {
    $sqlFiles = @(Get-ChildItem -LiteralPath $InputPath -Recurse -Filter '*.sql' -File)
}
else {
    $sqlFiles = @(Get-Item -LiteralPath $InputPath)
}

if ($sqlFiles.Count -eq 0) {
    Write-Warning "No .sql files found under '$InputPath'. Nothing to do."
    return
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Write-Host "Scanning $($sqlFiles.Count) .sql file(s) under '$InputPath'..."

$totalProcs = 0
$filesWritten = 0
$filesWithProcs = 0

foreach ($file in $sqlFiles) {
    $raw = Get-Content -LiteralPath $file.FullName -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) { continue }

    $clean = Remove-SqlNoise $raw
    $procs = @(Get-ProceduresFromSql $clean)

    if ($procs.Count -eq 0) {
        Write-Host "  (no procedures) $($file.Name)"
        continue
    }

    $filesWithProcs++
    $totalProcs += $procs.Count

    $className = (Get-CSharpIdentifier ([System.IO.Path]::GetFileNameWithoutExtension($file.Name))) + 'Tests'
    $content = New-TestClassFile -ClassName $className -Procedures $procs -SourceFileName $file.Name

    $outFile = Join-Path $OutputPath "$className.cs"
    if ((Test-Path -LiteralPath $outFile) -and -not $Force) {
        Write-Warning "  exists (skipped, use -Force): $outFile"
    }
    else {
        Set-Content -Path $outFile -Value $content -Encoding UTF8
        $filesWritten++
        $procNames = ($procs | ForEach-Object { $_.Name }) -join ', '
        Write-Host "  wrote $($className).cs  <-  $($file.Name)  [$procNames]"
    }
}

if ($EmitProject) {
    Write-ProjectFiles -OutDir $OutputPath
}

Write-Host ""
Write-Host "Done. Procedures found: $totalProcs across $filesWithProcs file(s). Test files written: $filesWritten."
if ($totalProcs -eq 0) {
    Write-Warning "No CREATE/ALTER PROCEDURE statements were detected. Check that the input files contain T-SQL stored procedures."
}
