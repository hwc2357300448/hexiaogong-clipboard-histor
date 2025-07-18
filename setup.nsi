; ClipboardHistory 安装脚本
; 使用 NSIS 创建

!define PRODUCT_NAME "剪贴板历史"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "何文才"
!define PRODUCT_WEB_SITE "https://github.com/clipboardhistory"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\ClipboardHistory.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; 包含现代UI
!include "MUI2.nsh"

; 设置压缩
SetCompressor lzma

; 安装程序信息
Name "${PRODUCT_NAME}"
OutFile "ClipboardHistory-Setup.exe"
InstallDir "$PROGRAMFILES\ClipboardHistory"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

; 界面设置
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; 欢迎页面
!insertmacro MUI_PAGE_WELCOME
; 许可协议页面
!insertmacro MUI_PAGE_LICENSE "license.txt"
; 组件页面
!insertmacro MUI_PAGE_COMPONENTS
; 目录页面
!insertmacro MUI_PAGE_DIRECTORY
; 安装页面
!insertmacro MUI_PAGE_INSTFILES
; 完成页面
!define MUI_FINISHPAGE_RUN "$INSTDIR\ClipboardHistory.exe"
!insertmacro MUI_PAGE_FINISH

; 卸载页面
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; 语言
!insertmacro MUI_LANGUAGE "SimpChinese"

; 安装类型
InstType "完整安装"
InstType "最小安装"

Section "主程序" SEC01
  SectionIn RO
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  
  ; 复制主程序文件
  File "publish\ClipboardHistory.exe"
  File "publish\ClipboardHistory.dll"
  File "publish\ClipboardHistory.runtimeconfig.json"
  File "publish\ClipboardHistory.deps.json"
  File "publish\ClipboardHistory.pdb"
  
  ; 复制依赖库文件
  File "publish\Hardcodet.NotifyIcon.Wpf.dll"
  File "publish\Microsoft.Data.Sqlite.dll"
  File "publish\SQLitePCLRaw.batteries_v2.dll"
  File "publish\SQLitePCLRaw.core.dll"
  File "publish\SQLitePCLRaw.provider.e_sqlite3.dll"
  
  ; 复制运行时库文件夹
  SetOutPath "$INSTDIR\runtimes"
  File /r "publish\runtimes\*"
  
  ; 创建快捷方式
  SetOutPath "$INSTDIR"
  CreateDirectory "$SMPROGRAMS\剪贴板历史"
  CreateShortCut "$SMPROGRAMS\剪贴板历史\剪贴板历史.lnk" "$INSTDIR\ClipboardHistory.exe"
  CreateShortCut "$DESKTOP\剪贴板历史.lnk" "$INSTDIR\ClipboardHistory.exe"
SectionEnd

Section "开机启动" SEC02
  SectionIn 1
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "ClipboardHistory" "$INSTDIR\ClipboardHistory.exe"
SectionEnd

Section -AdditionalIcons
  SetOutPath $INSTDIR
  CreateShortCut "$SMPROGRAMS\剪贴板历史\卸载.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\ClipboardHistory.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\ClipboardHistory.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

; 组件描述
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC01} "安装主程序文件"
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC02} "开机自动启动程序"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section Uninstall
  ; 删除主程序文件
  Delete "$INSTDIR\ClipboardHistory.exe"
  Delete "$INSTDIR\ClipboardHistory.dll"
  Delete "$INSTDIR\ClipboardHistory.runtimeconfig.json"
  Delete "$INSTDIR\ClipboardHistory.deps.json"
  Delete "$INSTDIR\ClipboardHistory.pdb"
  Delete "$INSTDIR\uninst.exe"
  
  ; 删除依赖库文件
  Delete "$INSTDIR\Hardcodet.NotifyIcon.Wpf.dll"
  Delete "$INSTDIR\Microsoft.Data.Sqlite.dll"
  Delete "$INSTDIR\SQLitePCLRaw.batteries_v2.dll"
  Delete "$INSTDIR\SQLitePCLRaw.core.dll"
  Delete "$INSTDIR\SQLitePCLRaw.provider.e_sqlite3.dll"
  
  ; 删除运行时库文件夹
  RMDir /r "$INSTDIR\runtimes"
  
  ; 删除用户数据（可选）
  ; Delete "$APPDATA\ClipboardHistory\*.*"
  ; RMDir "$APPDATA\ClipboardHistory"
  
  ; 删除快捷方式
  Delete "$SMPROGRAMS\剪贴板历史\*.*"
  Delete "$DESKTOP\剪贴板历史.lnk"
  
  ; 删除目录
  RMDir "$SMPROGRAMS\剪贴板历史"
  RMDir "$INSTDIR"
  
  ; 删除注册表项
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "ClipboardHistory"
  
  SetAutoClose true
SectionEnd