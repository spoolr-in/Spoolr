[Setup]
AppName=SpoolrStation
AppVersion=1.0
DefaultDirName={autopf64}\SpoolrStation
DefaultGroupName=SpoolrStation
OutputBaseFilename=SpoolrStation-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "D:\Spoolr Project\Spoolr\spoolr-station-build\SpoolrStation\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SpoolrStation"; Filename: "{app}\SpoolrStation.exe"
Name: "{commondesktop}\SpoolrStation"; Filename: "{app}\SpoolrStation.exe"

[Run]
Filename: "{app}\SpoolrStation.exe"; Description: "Launch SpoolrStation"; Flags: nowait postinstall skipifsilent
