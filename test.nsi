!include "Sections.nsh"
!include "LogicLib.nsh"

Name "OpenRPA"
OutFile "OpenRPA.exe"
InstallDir $PROGRAMFILES\OpenRPA

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\OpenRPA" "Install_Dir"
; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------

; Pages
Var hasoffice
var version
Page Custom CheckForOffice
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

Function CheckForOffice
  ClearErrors
  
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\16.0\Excel\InstallRoot" "Path"
  MessageBox MB_OKCANCEL $0
  ${If} ${Errors}
    StrCpy $hasoffice "false"
    ClearErrors
  ${Else}
    StrCpy $hasoffice "true"
  ${EndIf}
  
  ${If} $hasoffice == "false"
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\15.0\Excel\InstallRoot" "Path"
    MessageBox MB_OKCANCEL $0
    ${If} ${Errors}
      StrCpy $hasoffice "false"
    ${Else}
      StrCpy $hasoffice "true"
    ${EndIf}
  ${EndIf}
  ; https://nsis.sourceforge.io/Managing_Sections_on_Runtime
  ${If} $hasoffice == "false"
    SectionSetFlags 2 0 ; unselect office, when not found
  ${EndIf}
  MessageBox MB_OKCANCEL $hasoffice

  SectionSetFlags 11 0 ; unselect Java per default
  SectionSetFlags 12 0 ; unselect High Density robors per default
  SectionSetFlags 13 0 ; unselect Elis Rossum per default

FunctionEnd

Section "Base robot files" ; section 0
  SectionIn RO


  SetOutPath $INSTDIR\Updater
  File /r "C:\code\openrpa\OpenRPA.Updater\bin\PrepInstaller\net462\*"
  SetOutPath $INSTDIR
  
  File /r "C:\code\openrpa\OpenRPA\bin\PrepInstaller\net462\*"
  
  WriteRegStr HKLM SOFTWARE\OpenRPA "Install_Dir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRPA" "DisplayName" "OpenRPA"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRPA" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRPA" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRPA" "NoRepair" 1
  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Start Menu Shortcuts" ; section 1
  CreateDirectory "$SMPROGRAMS\OpenRPA"
  CreateShortcut "$SMPROGRAMS\OpenRPA\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortcut "$SMPROGRAMS\OpenRPA\OpenRPA.lnk" "$INSTDIR\OpenRPA.exe" "" "$INSTDIR\OpenRPA.exe" 0
SectionEnd

Section "Uninstall"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRPA"
  DeleteRegKey HKLM SOFTWARE\OpenRPA
  Delete "$SMPROGRAMS\OpenRPA\*.*"
  RMDir "$SMPROGRAMS\OpenRPA"
  RMDir /r /REBOOTOK "$INSTDIR"
SectionEnd

Section "Office" ; section 2
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\*"
SectionEnd
