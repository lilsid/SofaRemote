; Sofa Remote - Inno Setup Installer Script
; This creates a professional Windows installer with Program Files installation,
; Start Menu shortcuts, optional desktop shortcut, and auto-start capability

#define MyAppName "Sofa Remote"
#define MyAppVersion "2025.12.27.1"
#define MyAppPublisher "Sofa Remote"
#define MyAppURL "https://github.com/lilsid/SofaRemote"
#define MyAppExeName "SofaRemote.exe"
#define BonjourURL "https://download.info.apple.com/Mac_OS_X/061-8098.20100603.gthyu/BonjourPSSetup.exe"

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
var
  DownloadPage: TDownloadWizardPage;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded %s', [FileName]));
  Result := True;
end;

function IsBonjourInstalled: Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{6E3610B2-430D-4EB0-81E3-2B57E8B9DE8D}';
  Result := RegKeyExists(HKLM, UninstallKey) or RegKeyExists(HKLM64, UninstallKey);
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpReady then begin
    if not IsBonjourInstalled then begin
      DownloadPage.Clear;
      DownloadPage.Add('{#BonjourURL}', 'BonjourPSSetup.exe', '');
      DownloadPage.Show;
      try
        try
          DownloadPage.Download;
          Result := True;
        except
          if DownloadPage.AbortedByUser then
            Log('Bonjour download cancelled by user')
          else
            SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
          Result := False;
        end;
      finally
        DownloadPage.Hide;
      end;
    end else begin
      Log('Bonjour already installed, skipping download');
      Result := True;
    end;
  end else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  BonjourPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Install Bonjour if it was downloaded
    BonjourPath := ExpandConstant('{tmp}\BonjourPSSetup.exe');
    if FileExists(BonjourPath) and not IsBonjourInstalled then
    begin
      Log('Installing Bonjour Print Services...');
      // Run without /quiet so user can see it installing
      if Exec(BonjourPath, '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
        Log('Bonjour installation completed with code: ' + IntToStr(ResultCode))
      else
        Log('Failed to execute Bonjour installer');
    end;
  end;
end;
