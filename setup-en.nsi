; ClipboardHistory Installation Script
; Created with NSIS

!define PRODUCT_NAME "HeXiaoGong-ClipboardHistory"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "HeXiaoGong"
!define PRODUCT_WEB_SITE "https://github.com/hwc2357300448/hexiaogong-clipboard-histor"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\ClipboardHistory.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Include Modern UI
!include "MUI2.nsh"

; Set compression
SetCompressor lzma

; Installer info
Name "${PRODUCT_NAME}"
OutFile "ClipboardHistory-Setup.exe"
InstallDir "$PROGRAMFILES\HeXiaoGong-ClipboardHistory"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

; Interface settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!insertmacro MUI_PAGE_WELCOME
; License page
!insertmacro MUI_PAGE_LICENSE "LICENSE"
; Components page
!insertmacro MUI_PAGE_COMPONENTS
; Directory page
!insertmacro MUI_PAGE_DIRECTORY
; Install page
!insertmacro MUI_PAGE_INSTFILES
; Finish page
!define MUI_FINISHPAGE_RUN "$INSTDIR\ClipboardHistory.exe"
!insertmacro MUI_PAGE_FINISH

; Uninstall pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Languages
!insertmacro MUI_LANGUAGE "SimpChinese"

; Install types
InstType "Complete Install"
InstType "Minimal Install"

Section "Main Program" SEC01
  SectionIn RO
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  
  ; Copy main program files
  File "publish\ClipboardHistory.exe"
  File "publish\ClipboardHistory.dll"
  File "publish\ClipboardHistory.runtimeconfig.json"
  File "publish\ClipboardHistory.deps.json"
  File "publish\ClipboardHistory.pdb"
  
  ; Copy dependency libraries
  File "publish\Hardcodet.NotifyIcon.Wpf.dll"
  File "publish\Microsoft.Data.Sqlite.dll"
  File "publish\SQLitePCLRaw.batteries_v2.dll"
  File "publish\SQLitePCLRaw.core.dll"
  File "publish\SQLitePCLRaw.provider.e_sqlite3.dll"
  
  ; Copy runtime libraries folder
  SetOutPath "$INSTDIR\runtimes"
  File /r "publish\runtimes\*"
  
  ; Create shortcuts
  SetOutPath "$INSTDIR"
  CreateDirectory "$SMPROGRAMS\HeXiaoGong-ClipboardHistory"
  CreateShortCut "$SMPROGRAMS\HeXiaoGong-ClipboardHistory\ClipboardHistory.lnk" "$INSTDIR\ClipboardHistory.exe"
  CreateShortCut "$DESKTOP\HeXiaoGong-ClipboardHistory.lnk" "$INSTDIR\ClipboardHistory.exe"
SectionEnd

Section "Auto Start" SEC02
  SectionIn 1
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "ClipboardHistory" "$INSTDIR\ClipboardHistory.exe"
SectionEnd

Section -AdditionalIcons
  SetOutPath $INSTDIR
  CreateShortCut "$SMPROGRAMS\HeXiaoGong-ClipboardHistory\Uninstall.lnk" "$INSTDIR\uninst.exe"
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

; Component descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC01} "Install main program files"
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC02} "Auto start program on system boot"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section Uninstall
  ; Delete main program files
  Delete "$INSTDIR\ClipboardHistory.exe"
  Delete "$INSTDIR\ClipboardHistory.dll"
  Delete "$INSTDIR\ClipboardHistory.runtimeconfig.json"
  Delete "$INSTDIR\ClipboardHistory.deps.json"
  Delete "$INSTDIR\ClipboardHistory.pdb"
  Delete "$INSTDIR\uninst.exe"
  
  ; Delete dependency libraries
  Delete "$INSTDIR\Hardcodet.NotifyIcon.Wpf.dll"
  Delete "$INSTDIR\Microsoft.Data.Sqlite.dll"
  Delete "$INSTDIR\SQLitePCLRaw.batteries_v2.dll"
  Delete "$INSTDIR\SQLitePCLRaw.core.dll"
  Delete "$INSTDIR\SQLitePCLRaw.provider.e_sqlite3.dll"
  
  ; Delete runtime libraries folder
  RMDir /r "$INSTDIR\runtimes"
  
  ; Delete user data (optional)
  ; Delete "$APPDATA\ClipboardHistory\*.*"
  ; RMDir "$APPDATA\ClipboardHistory"
  
  ; Delete shortcuts
  Delete "$SMPROGRAMS\HeXiaoGong-ClipboardHistory\*.*"
  Delete "$DESKTOP\HeXiaoGong-ClipboardHistory.lnk"
  
  ; Delete directories
  RMDir "$SMPROGRAMS\HeXiaoGong-ClipboardHistory"
  RMDir "$INSTDIR"
  
  ; Delete registry keys
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "ClipboardHistory"
  
  SetAutoClose true
SectionEnd