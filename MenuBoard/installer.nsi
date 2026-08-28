; Menu Board - one-click Windows installer
;
; Built for non-technical users: Welcome -> Install -> Finish (launch checked).
; No directory page, no admin prompt (per-user install), autostart on boot
; pre-configured via the same HKCU Run value the in-app checkbox manages.
;
; Build (after publishing the payload):
;   dotnet publish src\MenuBoard -c Release -r win-x64 --self-contained true -o payload
;   makensis installer.nsi
; Output: MenuBoardSetup.exe

!include "MUI2.nsh"

Name "Menu Board"
OutFile "MenuBoardSetup.exe"
Unicode True
RequestExecutionLevel user
InstallDir "$LOCALAPPDATA\MenuBoard"
SetCompressor /SOLID lzma

!define APP_NAME "Menu Board"
!define RUN_KEY "Software\Microsoft\Windows\CurrentVersion\Run"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\MenuBoard"
!define VERSION "1.0.0"

; --- UI: keep it minimal -------------------------------------------------
!define MUI_WELCOMEPAGE_TITLE "Menu Board Setup"
!define MUI_WELCOMEPAGE_TEXT "This installs Menu Board on this PC.$\r$\n$\r$\nAfter installing, the menu boards will start by themselves every time the PC is turned on.$\r$\n$\r$\nClick Install to continue."
!define MUI_FINISHPAGE_RUN "$INSTDIR\MenuBoard.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Start Menu Board now"
!define MUI_FINISHPAGE_TITLE "All done"
!define MUI_FINISHPAGE_TEXT "Menu Board is installed and will start automatically when the PC is turned on.$\r$\n$\r$\nTip: to edit the menus later, press Esc on a menu screen or open Menu Board from the Start Menu."

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

; --- Install -------------------------------------------------------------
Section "Menu Board"
  SetOutPath "$INSTDIR"

  ; App payload (self-contained; needs no .NET install on this PC).
  ; Existing menu data (menuboard.db, Images, settings.json) is never part
  ; of the payload, so reinstalling/upgrading keeps the store's menu.
  File /r "payload\*.*"

  ; Start on boot - same value name and quoting the in-app
  ; "Start Menu Board automatically when Windows starts" checkbox uses,
  ; so the checkbox shows as ON and can still turn it off.
  WriteRegStr HKCU "${RUN_KEY}" "MenuBoard" '"$INSTDIR\MenuBoard.exe"'

  ; Start Menu shortcut + uninstaller
  CreateShortcut "$SMPROGRAMS\Menu Board.lnk" "$INSTDIR\MenuBoard.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Apps & Features entry
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${UNINST_KEY}" "Publisher" "Brandon Stroidl"
  WriteRegStr HKCU "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\MenuBoard.exe"
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoRepair" 1
SectionEnd

; --- Uninstall -----------------------------------------------------------
Section "Uninstall"
  ; Stop autostart and shortcuts first
  DeleteRegValue HKCU "${RUN_KEY}" "MenuBoard"
  Delete "$SMPROGRAMS\Menu Board.lnk"

  ; Offer to keep the menu itself (database + photos + settings)
  MessageBox MB_YESNO|MB_ICONQUESTION \
    "Keep the menu data (items, prices, photos)?$\r$\n$\r$\nChoose Yes to keep it for a future reinstall, No to delete everything." \
    /SD IDYES IDYES KeepData

  RMDir /r "$INSTDIR"
  Goto DoneData

KeepData:
  ; Delete application files but preserve menuboard.db, Images\, settings.json
  Delete "$INSTDIR\*.exe"
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.json.bak"
  Delete "$INSTDIR\MenuBoard.deps.json"
  Delete "$INSTDIR\MenuBoard.runtimeconfig.json"
  Delete "$INSTDIR\*.pdb"
  RMDir /r "$INSTDIR\runtimes"
  RMDir /r "$INSTDIR\cs"
  RMDir /r "$INSTDIR\de"
  RMDir /r "$INSTDIR\es"
  RMDir /r "$INSTDIR\fr"
  RMDir /r "$INSTDIR\it"
  RMDir /r "$INSTDIR\ja"
  RMDir /r "$INSTDIR\ko"
  RMDir /r "$INSTDIR\pl"
  RMDir /r "$INSTDIR\pt-BR"
  RMDir /r "$INSTDIR\ru"
  RMDir /r "$INSTDIR\tr"
  RMDir /r "$INSTDIR\zh-Hans"
  RMDir /r "$INSTDIR\zh-Hant"

DoneData:
  DeleteRegKey HKCU "${UNINST_KEY}"
SectionEnd
