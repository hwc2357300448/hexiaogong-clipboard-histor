; 何小工-剪贴板历史 v1.1.0 安装脚本
; 使用 Inno Setup 编译此脚本生成安装程序

#define MyAppName "何小工-剪贴板历史"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "何小工(HeXiaoGong)"
#define MyAppURL "https://github.com/hexiaogong/hexiaogong-clipboard-histor"
#define MyAppExeName "ClipboardHistory.exe"
#define MyAppId "{{A8F2B9C3-4D5E-6F7A-8B9C-0D1E2F3A4B5C}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
InfoBeforeFile=README.md
OutputDir=Release
OutputBaseFilename=ClipboardHistory-v{#MyAppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=ClipboardHistory\icon.ico
;WizardImageFile=compiler:WizModernImage-IS.bmp
;WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "autostart"; Description: "开机自动启动（需要管理员权限）"; GroupDescription: "其他选项:"; Flags: unchecked

[Files]
Source: "ClipboardHistory\bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\使用说明"; Filename: "{app}\README.md"
Name: "{group}\更新日志"; Filename: "{app}\CHANGELOG.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runasoriginaluser
; 自动启动任务由程序内部的设置窗口管理，安装程序不直接创建

[Registry]
; 开机自启动（仅当用户勾选时）
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[UninstallDelete]
Type: filesandordirs; Name: "{app}\clipboard_history.db"
Type: filesandordirs; Name: "{app}\settings.json"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // 安装完成后的自定义操作
    Log('安装完成');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  MsgBoxResult: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // 询问是否删除用户数据
    MsgBoxResult := MsgBox('是否同时删除剪贴板历史数据和设置？' + #13#10 + #13#10 +
                          '点击"是"将删除所有历史记录和设置。' + #13#10 +
                          '点击"否"将保留数据，以便将来重新安装时使用。',
                          mbConfirmation, MB_YESNO);

    if MsgBoxResult = IDNO then
    begin
      // 用户选择保留数据，取消删除
      Log('用户选择保留数据');
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  UninstallString: String;
begin
  // 检查是否已安装旧版本
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1',
                         'UninstallString', UninstallString) then
  begin
    if MsgBox('检测到已安装的版本，需要先卸载。是否继续？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
      Result := True;
    end
    else
      Result := False;
  end
  else
    Result := True;
end;
