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
  ${If} ${Errors}
    StrCpy $hasoffice "false"
    ClearErrors
  ${Else}
    StrCpy $hasoffice "true"
  ${EndIf}
  
  ${IF} $hasoffice == "false"
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\15.0\Excel\InstallRoot" "Path"
    ${If} ${Errors}
      StrCpy $hasoffice "false"
    ${Else}
      StrCpy $hasoffice "true"
    ${EndIf}
  ${EndIf}
  ; https://nsis.sourceforge.io/Managing_Sections_on_Runtime
  ${IF} $hasoffice == "false"
    SectionSetFlags 2 0 ; unselect office, when not found
  ${EndIf}


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
Section "Forms" ; section 3
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\*"
SectionEnd
Section "IE" ; section 4
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.IE\bin\PrepInstaller\net462\*"
SectionEnd
Section "Chrome and Firefox (NM)" ; section 5
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\*"
SectionEnd
Section "Image recognition and OCR" ; section 6
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\*"
SectionEnd
Section "Generic Scripting support" ; section 7
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\*"
SectionEnd
Section "AviRecorder" ; section 8
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\*"
SectionEnd
Section "OpenFlowDB" ; section 9
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.OpenFlowDB\bin\PrepInstaller\net462\*"
SectionEnd
Section "FileWatcher" ; section 10
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.FileWatcher\bin\PrepInstaller\net462\*"
SectionEnd

Section "Java support" ; section 11
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\*"
SectionEnd
Section "High Density robors using Remote Desktop" ; section 12
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\*"
SectionEnd
Section "Elis Rossum" ; section 13
  SetOutPath $INSTDIR
  File /r "C:\code\openrpa\OpenRPA.Elis.Rossum\bin\PrepInstaller\net462\*"
SectionEnd





