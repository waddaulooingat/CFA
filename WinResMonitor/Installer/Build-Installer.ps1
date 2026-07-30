# WinResMonitor — Build + Package
# Run from the Installer\ folder as Administrator.
# Requires: .NET 8 SDK, Inno Setup 6 (https://jrsoftware.org/isinfo.php)

$ErrorActionPreference = "Stop"
$root    = Split-Path $PSScriptRoot -Parent
$iscc    = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "`n=== Building Service ===" -ForegroundColor Cyan
dotnet publish "$root\WinResMonitor.Service\WinResMonitor.Service.csproj" `
  -c Release --self-contained false -o "$root\WinResMonitor.Service\bin\Release\net8.0-windows\publish"

Write-Host "`n=== Building UI ===" -ForegroundColor Cyan
dotnet publish "$root\WinResMonitor.UI\WinResMonitor.UI.csproj" `
  -c Release --self-contained false -o "$root\WinResMonitor.UI\bin\Release\net8.0-windows\publish"

Write-Host "`n=== Running Inno Setup ===" -ForegroundColor Cyan
if (-not (Test-Path $iscc)) {
  Write-Error "Inno Setup not found at $iscc. Download from https://jrsoftware.org/isinfo.php"
  exit 1
}

& $iscc "$PSScriptRoot\WinResMonitor.iss"

Write-Host "`n=== Done ===" -ForegroundColor Green
Write-Host "Installer is in: $PSScriptRoot\Output\" -ForegroundColor Green
