; PowerProfile standard Windows installer
#define AppName "PowerProfile"
#define AppVersion "1.0.0"
#define AppPublisher "PowerProfile"
#define AppExeName "PowerProfile.exe"
#define AppId "{{A4F9C2D1-8E37-4F2A-9B65-1C7D3E5A8F20}}"
#define SourceDir "..\scripts\dist\PowerProfile.App"
#define FlutterDir "..\scripts\dist\PowerProfile.FlutterUI"
#define PowerChangeDir "..\backup\PowerChange"
#define IconFile "..\assets\PowerProfile.ico"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\scripts\dist
OutputBaseFilename=PowerProfile-Setup-{#AppVersion}
SetupIconFile={#IconFile}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#AppName}

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式:"; Flags: unchecked
Name: "startup"; Description: "登录 Windows 时自动启动"; GroupDescription: "启动选项:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#FlutterDir}\*"; DestDir: "{app}\PowerProfile.FlutterUI"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PowerChangeDir}\RunHidden.vbs"; DestDir: "{autopf}\PowerChange"; Flags: ignoreversion
Source: "{#PowerChangeDir}\PowerProfileSwitch.ps1"; DestDir: "{autopf}\PowerChange"; Flags: ignoreversion
Source: "{#PowerChangeDir}\RefreshRateChanger.exe"; DestDir: "{autopf}\PowerChange"; Flags: ignoreversion
Source: "{#PowerChangeDir}\PowerProfileLauncher.exe"; DestDir: "{autopf}\PowerChange"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: "{app}\{#AppExeName}"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "启动 {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\PowerProfile"
