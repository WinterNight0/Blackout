[Setup]
AppName=Blackout
AppVersion=1.0
DefaultDirName=C:\Blackout\Program
DefaultGroupName=Blackout
OutputBaseFilename=BlackoutInstaller
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\blackout.exe

[Files]
Source: "D:\Blackout\Blackout Apps\blackout.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Blackout"; Filename: "{app}\blackout.exe"
Name: "{userdesktop}\Blackout"; Filename: "{app}\blackout.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop Shortcut"; GroupDescription: "Additional icons"; Flags: unchecked

[Code]
const
  LauncherName = 'blackout.bat';
  LauncherFolderConst = '{localappdata}\Microsoft\WindowsApps';

procedure CreateLauncher();
var
  LauncherFolder, LauncherPath, TargetPath: string;
begin
  LauncherFolder := ExpandConstant(LauncherFolderConst);
  ForceDirectories(LauncherFolder);

  LauncherPath := LauncherFolder + '\' + LauncherName;
  TargetPath := ExpandConstant('{app}\blackout.exe');

  if not FileExists(LauncherPath) then
    SaveStringToFile(LauncherPath,
      '@echo off' + #13#10 +
      '"' + TargetPath + '" %*', // forwards all arguments to blackout.exe
      False
    );
end;

procedure DeleteLauncher();
var
  LauncherPath: string;
begin
  LauncherPath := ExpandConstant(LauncherFolderConst + '\' + LauncherName);
  if FileExists(LauncherPath) then
    DeleteFile(LauncherPath);
end;

procedure CreateScheduledTasks();
var
  ResultCode: Integer;
  exePath: string;
begin
  exePath := ExpandConstant('{app}\blackout.exe');

  // Status task (runs at logon, user-level)
  Exec('schtasks.exe',
    '/create /f /tn "Blackout_Status" /tr "' + exePath + ' status" /sc onlogon /rl LIMITED',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Shutdown/Hibernate task renamed to Blackout_Schedule (daily, user-level)
  Exec('schtasks.exe',
    '/create /f /tn "Blackout_Schedule" /tr "' + exePath + ' now" /sc daily /st 02:00 /rl LIMITED',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure DeleteScheduledTasks();
var
  ResultCode: Integer;
begin
  Exec('schtasks.exe', '/delete /f /tn "Blackout_Status"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('schtasks.exe', '/delete /f /tn "Blackout_Schedule"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    CreateLauncher();
    CreateScheduledTasks();
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DeleteLauncher();
    DeleteScheduledTasks();
  end;
end;
