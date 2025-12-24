; Sofa Remote - Inno Setup Installer Script
; This creates a professional Windows installer with Program Files installation,
; Start Menu shortcuts, optional desktop shortcut, and auto-start capability

#define MyAppName "Sofa Remote"
#define MyAppVersion "2025.12.24.1"
#define MyAppPublisher "Sofa Remote"
#define MyAppURL "https://github.com/yourusername/SofaRemote"
#define MyAppExeName "SofaRemote.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{A8B3C2D1-5E4F-6G7H-8I9J-0K1L2M3N4O5P}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
OutputDir=publish\installers
OutputBaseFilename=SofaRemote-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=icons\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Launch {#MyAppName} at Windows startup"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
; Include all files from the build output
Source: "publish\single-file\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\single-file\icons\*"; DestDir: "{app}\icons"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Configure firewall and launch {#MyAppName}"; Flags: postinstall nowait skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Optionally run the app with admin to configure firewall
    // This is handled by the [Run] section
  end;
end;
