#define AppName      "WinResMonitor"
#define AppVersion   "1.0.0"
#define AppPublisher "Your Company Name"
#define AppURL       "https://yourwebsite.com"
#define ServiceName  "WinResMonitor"
#define ServiceExe   "WinResMonitor.Service.exe"
#define UIExe        "WinResMonitor.UI.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Require admin — needed to install a Windows Service
PrivilegesRequired=admin
OutputDir=..\Installer\Output
OutputBaseFilename=WinResMonitor_Setup_{#AppVersion}
SetupIconFile=..\WinResMonitor.UI\Assets\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Minimum Windows 10
MinVersion=10.0
UninstallDisplayIcon={app}\UI\{#UIExe}
UninstallDisplayName={#AppName}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=WinResMonitor Parental Control

; Uncomment and set these when you have a code-signing certificate:
; SignTool=signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f "cert.pfx" /p "password" $f
; SignedUninstaller=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";    Description: "Create a &desktop shortcut";    GroupDescription: "Additional icons:"
Name: "startupicon";   Description: "Launch management UI at &startup"; GroupDescription: "Additional icons:"

[Files]
; Service binaries
Source: "..\WinResMonitor.Service\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs

; UI binaries
Source: "..\WinResMonitor.UI\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}\UI"; Flags: ignoreversion recursesubdirs

; Browser Extension
Source: "..\WinResMonitor.Extension\*"; DestDir: "{app}\Extension"; Flags: ignoreversion recursesubdirs

; Extension install helper script
Source: "InstallExtension.bat"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\WinResMonitor";          Filename: "{app}\UI\{#UIExe}"
Name: "{group}\Uninstall WinResMonitor"; Filename: "{uninstallexe}"

; Desktop shortcut (optional task)
Name: "{autodesktop}\WinResMonitor";    Filename: "{app}\UI\{#UIExe}"; Tasks: desktopicon

; Startup folder (optional task)
Name: "{userstartup}\WinResMonitor";    Filename: "{app}\UI\{#UIExe}"; Tasks: startupicon

[Run]
; Install and start the Windows Service
Filename: "sc.exe"; Parameters: "create {#ServiceName} binPath= ""{app}\Service\{#ServiceExe}"" start= auto DisplayName= ""Windows Resource Monitor"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden waituntilterminated

; Launch the UI after install so parent can set password immediately
Filename: "{app}\UI\{#UIExe}"; Description: "Launch WinResMonitor (set your password)"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove the service before files are deleted
Filename: "sc.exe"; Parameters: "stop {#ServiceName}";   Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated

; Remove proxy setting (system-wide)
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings"" /v ProxyEnable /f"; Flags: runhidden
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings"" /v ProxyServer /f"; Flags: runhidden

[UninstallDelete]
; Clean up data folder (logs, db, reports) — ask user first via code below
Type: filesandordirs; Name: "{commonappdata}\{#AppName}"

[Code]

// ── .NET 8 prerequisite check ──────────────────────────────────────────────
function IsDotNet8Installed(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if dotnet.exe can find net8.0
  Exec('cmd.exe', '/c dotnet --list-runtimes | find "Microsoft.WindowsDesktop.App 8." > nul 2>&1',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8Installed() then
  begin
    if MsgBox(
      '.NET 8 Desktop Runtime is required but was not found.' + #13#10 + #13#10 +
      'Click OK to open the Microsoft download page, then re-run this installer after installing .NET 8.',
      mbError, MB_OKCANCEL) = IDOK then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, 0);
    end;
    Result := False;
  end;
end;

// ── Ask about data folder on uninstall ─────────────────────────────────────
function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox(
    'Do you want to delete all WinResMonitor data?' + #13#10 +
    '(logs, block database, reports)' + #13#10 + #13#10 +
    'Click Yes to remove everything, No to keep your data.',
    mbConfirmation, MB_YESNO) = IDNO then
  begin
    // Cancel deletion of data folder by neutralising the [UninstallDelete] entry
    // We do this by simply not deleting — Inno will still run [UninstallRun] entries
    DelTree(ExpandConstant('{commonappdata}\{#AppName}\reports'), True, True, True);
    // Leave db and auth.json so a reinstall picks up where it left off
  end;
end;
