; Inno Setup script for the Windows .exe installer, built against a `dotnet publish`
; self-contained/single-file output — see .github/workflows/release.yml.
; Compiled with ISCC.exe, passing /DMyAppVersion=<version> /DPublishDir=<publish dir>.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\..\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

#define MyAppName "Azure Key Vault Desktop"
#define MyAppExeName "AzureKeyVaultDesktop.exe"

[Setup]
AppId={{B0646F9E-7F01-4598-A2D2-743383B28AB3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppName}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename=AzureKeyVaultDesktop-{#MyAppVersion}-Setup
OutputDir={#OutputDir}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
