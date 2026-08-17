; Asset Management - Windows installer
; Built with NSIS 3 + Modern UI 2
;
; Expects self-contained single-file publishes to already exist at:
;   dist/win/cli/asset.exe
;   dist/win/mcp/AssetManagement.Mcp.exe
; (see PLAN-PUBLISH.md, section 2)
;
; Build: makensis installer/windows/setup.nsi

!define PRODUCT_NAME "Asset Management"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "Michael Petty"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AssetManagement"

!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"
!include "WinMessages.nsh"

Name "${PRODUCT_NAME}"
OutFile "..\..\dist\win\AssetManagementSetup.exe"
InstallDir "$PROGRAMFILES64\AssetManagement"
InstallDirRegKey HKLM "Software\AssetManagement" "InstallDir"
RequestExecutionLevel admin
ShowInstDetails show
ShowUnInstDetails show

Var LibraryPath
Var LibraryPathTextBox

;--------------------------------
; UI configuration

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
Page custom LibraryPageCreate LibraryPageLeave
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\MCP-CONFIG.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Show MCP configuration snippet"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Custom "Library location" page

Function .onInit
  StrCpy $LibraryPath "$PROFILE\AssetLibrary"
FunctionEnd

Function LibraryPageCreate
  !insertmacro MUI_HEADER_TEXT "Library Location" "Choose where your asset library will be created."
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0 0 100% 24u "Select the folder where your asset library (files and catalog database) will live:"
  Pop $0

  ${NSD_CreateDirRequest} 0 30u 74% 12u "$LibraryPath"
  Pop $LibraryPathTextBox

  ${NSD_CreateBrowseButton} 76% 29u 24% 14u "Browse..."
  Pop $0
  ${NSD_OnClick} $0 LibraryPageBrowse

  nsDialogs::Show
FunctionEnd

Function LibraryPageBrowse
  nsDialogs::SelectFolderDialog "Select Library Folder" "$LibraryPath"
  Pop $0
  ${If} $0 != error
    ${NSD_SetText} $LibraryPathTextBox "$0"
  ${EndIf}
FunctionEnd

Function LibraryPageLeave
  ${NSD_GetText} $LibraryPathTextBox $LibraryPath
  ${If} $LibraryPath == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "Please choose a library location."
    Abort
  ${EndIf}
FunctionEnd

;--------------------------------
; PATH helpers (install)
; Classic NSIS wiki StrStr - returns substring from match position, or "" if not found

Function StrStr
  Exch $R1 ; substring to find
  Exch
  Exch $R2 ; string to search
  Push $R3
  Push $R4
  Push $R5
  StrLen $R3 $R1
  StrCpy $R4 0
  loop:
    StrCpy $R5 $R2 $R3 $R4
    StrCmp $R5 $R1 done
    StrCmp $R5 "" notfound
    IntOp $R4 $R4 + 1
    Goto loop
  notfound:
    StrCpy $R1 ""
    Goto end
  done:
    StrCpy $R1 $R2 "" $R4
  end:
  Pop $R5
  Pop $R4
  Pop $R3
  Pop $R2
  Exch $R1
FunctionEnd

Function AddToPath
  ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"

  Push $0
  Push "$INSTDIR"
  Call StrStr
  Pop $1
  StrCmp $1 "" 0 AddToPath_done

  StrCmp $0 "" 0 AddToPath_append
  StrCpy $0 "$INSTDIR"
  Goto AddToPath_write

  AddToPath_append:
  StrCpy $1 $0 1 -1
  StrCmp $1 ";" 0 +2
  StrCpy $0 $0 -1
  StrCpy $0 "$0;$INSTDIR"

  AddToPath_write:
  WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path" "$0"
  SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000

  AddToPath_done:
FunctionEnd

;--------------------------------
; PATH helpers (uninstall)

Function un.StrStr
  Exch $R1
  Exch
  Exch $R2
  Push $R3
  Push $R4
  Push $R5
  StrLen $R3 $R1
  StrCpy $R4 0
  loop:
    StrCpy $R5 $R2 $R3 $R4
    StrCmp $R5 $R1 done
    StrCmp $R5 "" notfound
    IntOp $R4 $R4 + 1
    Goto loop
  notfound:
    StrCpy $R1 ""
    Goto end
  done:
    StrCpy $R1 $R2 "" $R4
  end:
  Pop $R5
  Pop $R4
  Pop $R3
  Pop $R2
  Exch $R1
FunctionEnd

Function un.RemoveFromPath
  ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"

  Push $0
  Push "$INSTDIR"
  Call un.StrStr
  Pop $1
  StrCmp $1 "" un.RemoveFromPath_done

  ; $1 = "$INSTDIR;rest" or "$INSTDIR" (tail match from the Path start position)
  ; Rebuild by removing exactly one "$INSTDIR" segment plus an adjoining separator.
  StrLen $2 $INSTDIR
  StrLen $3 $0
  StrLen $4 $1
  IntOp $5 $3 - $4 ; offset where $INSTDIR starts in $0
  StrCpy $6 $0 $5 ; text before the match
  IntOp $7 $5 + $2 ; offset just after $INSTDIR
  StrCpy $8 $0 "" $7 ; text after the match (may start with ";")
  StrCpy $9 $8 1
  StrCmp $9 ";" 0 +3
  StrCpy $8 $8 "" 1
  Goto +2
  StrCpy $6 $6 -1 ; drop trailing ";" left on the prefix side

  StrCpy $0 "$6$8"
  WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path" "$0"
  SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000

  un.RemoveFromPath_done:
FunctionEnd

;--------------------------------
; Install

Section "Install" SecInstall
  SetOutPath "$INSTDIR"
  File "..\..\dist\win\cli\asset.exe"
  File "..\..\dist\win\mcp\AssetManagement.Mcp.exe"

  WriteRegStr HKLM "Software\AssetManagement" "InstallDir" "$INSTDIR"
  Call AddToPath

  DetailPrint "Initializing asset library at $LibraryPath ..."
  nsExec::ExecToLog '"$SYSDIR\cmd.exe" /C ""$INSTDIR\asset.exe" init --path "$LibraryPath" --non-interactive > "$INSTDIR\MCP-CONFIG.txt" 2>&1"'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK|MB_ICONEXCLAMATION "Library initialization failed (exit code $0). You can run 'asset init' manually later from a new terminal."
  ${EndIf}

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
SectionEnd

;--------------------------------
; Uninstall
; Deliberately does not touch the library directory (assets/, catalog/) - user data.

Section "Uninstall"
  Delete "$INSTDIR\asset.exe"
  Delete "$INSTDIR\AssetManagement.Mcp.exe"
  Delete "$INSTDIR\MCP-CONFIG.txt"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  Call un.RemoveFromPath

  DeleteRegKey HKLM "Software\AssetManagement"
  DeleteRegKey HKLM "${UNINST_KEY}"
SectionEnd
