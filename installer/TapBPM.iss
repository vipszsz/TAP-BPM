; Inno Setup script for Tap BPM.
;
; Replaces the old SetupTAP.vdproj, a Visual Studio Installer project: that format is
; discontinued, needs a Visual Studio extension to open, and cannot be built from the
; command line or in CI. This one compiles anywhere ISCC.exe runs.
;
; Build (from the repository root):
;   dotnet publish src/TapBpm/TapBpm.csproj -c Release -o artifacts/publish
;   iscc installer\TapBPM.iss

#define AppName "Tap BPM"
#define AppVersion "2.0.0"
#define Publisher "Vipz"
#define AppUrl "https://github.com/vipszsz/TAP-BPM"
#define ExeName "TapBPM.exe"

[Setup]
; A fixed GUID identifies the product across versions, so installing 2.1 upgrades 2.0
; in place instead of leaving two copies behind.
AppId={{6E1B7C2A-3F44-4D5E-9A7C-2B8F1D6E9C31}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases

; Per-user install: no administrator prompt, and the app never needs elevated rights.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#Publisher}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
AllowNoIcons=yes

OutputDir=..\artifacts
OutputBaseFilename=TapBPM-{#AppVersion}-setup
SetupIconFile=..\src\TapBpm\Assets\tap-bpm.ico
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "Start Tap BPM when I sign in"; GroupDescription: "Startup"; Flags: unchecked

[Files]
; One self-contained executable: the user needs no .NET runtime installed.
Source: "..\artifacts\publish\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Preferences live outside the install directory, so remove them explicitly.
Type: filesandordirs; Name: "{userappdata}\{#Publisher}\{#AppName}"

[Code]
// Refuse to install over a running copy: replacing the executable would fail silently
// and leave a half-updated install.
function InitializeSetup(): Boolean;
begin
  Result := True;
  if CheckForMutexes('Local\Vipz.TapBPM.SingleInstance') then
  begin
    MsgBox('Tap BPM is currently running. Please close it and run the installer again.',
           mbError, MB_OK);
    Result := False;
  end;
end;
