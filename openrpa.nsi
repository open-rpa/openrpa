!include "Sections.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
; !include "StrFunc.nsh"
!include "StrRep.nsh"
!include "ReplaceInFile.nsh"

Name "OpenRPA"
OutFile "OpenRPA.exe"
InstallDir $PROGRAMFILES\OpenRPA
function .onInit
    ${If} ${RunningX64}
        SetRegView 64
        StrCpy $INSTDIR "$PROGRAMFILES64\OpenRPA"
    ${EndIf}
functionEnd

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\OpenRPA" "Install_Dir"
; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------

; Pages
Var hasoffice
;var version
Page Custom CheckForOffice
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

Function CheckForOffice
  ; https://stackoverflow.com/questions/27839860/nsis-check-if-registry-key-value-exists
  ClearErrors
  SetRegView 32
  
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\16.0\Excel\InstallRoot" "Path"
  ; MessageBox MB_OKCANCEL $0
  ${If} ${Errors}
    StrCpy $hasoffice "false"
    ClearErrors
  ${Else}
    StrCpy $hasoffice "true"
  ${EndIf}
  
  ${If} $hasoffice == "false"
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\15.0\Excel\InstallRoot" "Path"
    ${If} ${Errors}
      StrCpy $hasoffice "false"
    ${Else}
      StrCpy $hasoffice "true"
    ${EndIf}
  ${EndIf}

  ${If} $hasoffice == "false"
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\14.0\Excel\InstallRoot" "Path"
    ${If} ${Errors}
      StrCpy $hasoffice "false"
    ${Else}
      StrCpy $hasoffice "true"
    ${EndIf}
  ${EndIf}

  ${If} ${RunningX64}
    SetRegView 64

    ${If} $hasoffice == "false"
      ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\16.0\Excel\InstallRoot" "Path"
      ; MessageBox MB_OKCANCEL $0
      ${If} ${Errors}
        StrCpy $hasoffice "false"
        ClearErrors
      ${Else}
        StrCpy $hasoffice "true"
      ${EndIf}
    ${EndIf}
    
    ${If} $hasoffice == "false"
      ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\15.0\Excel\InstallRoot" "Path"
      ${If} ${Errors}
        StrCpy $hasoffice "false"
      ${Else}
        StrCpy $hasoffice "true"
      ${EndIf}
    ${EndIf}
    
    ${If} $hasoffice == "false"
      ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Office\14.0\Excel\InstallRoot" "Path"
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


    SetRegView LastUsed
  ${EndIf}


  SectionSetFlags 11 0 ; unselect Java per default
  SectionSetFlags 12 0 ; unselect High Density robots per default
  SectionSetFlags 13 0 ; unselect Elis Rossum per default
  SectionSetFlags 14 0 ; unselect Process Discovery per default

FunctionEnd

Section "Base robot files" ; section 0
  SectionIn RO

  SetOutPath $INSTDIR\Updater
  File /r "C:\code\openrpa\OpenRPA.Updater\bin\PrepInstaller\net462\*"
  SetOutPath $INSTDIR\Packages
  File "C:\code\openrpa\packages\*.nupkg"
  SetOutPath $INSTDIR
  
  File /r "C:\code\openrpa\OpenRPA\bin\PrepInstaller\net462\*"
  File /r "C:\code\openrpa\OpenRPA.Utilities\bin\PrepInstaller\net462\*"  

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
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\Microsoft.Office.Interop.Excel.dll"
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\Microsoft.Office.Interop.Outlook.dll"
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\Microsoft.Office.Interop.PowerPoint.dll"
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\Microsoft.Office.Interop.Word.dll"
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\OpenRPA.Office.dll"
  File "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\OpenRPA.Office.pdb"
  File /r "C:\code\openrpa\OpenRPA.Office\bin\PrepInstaller\net462\OpenRPA.Office.resources.dll"
SectionEnd
Section "Forms" ; section 3
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\ControlzEx.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\FastMember.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\Forge.Forms.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\Humanizer.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\MahApps.Metro.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\MaterialDesignColors.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\MaterialDesignThemes.Wpf.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\Microsoft.Win32.Primitives.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\OpenRPA.Forms.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\OpenRPA.Forms.pdb"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\ToastNotifications.dll"
  File "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\ToastNotifications.Messages.dll"
  File /r "C:\code\openrpa\OpenRPA.Forms\bin\PrepInstaller\net462\OpenRPA.Forms.resources.dll"
SectionEnd
Section "IE" ; section 4
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.IE\bin\PrepInstaller\net462\OpenRPA.IE.dll"
  File "C:\code\openrpa\OpenRPA.IE\bin\PrepInstaller\net462\OpenRPA.IE.pdb"
  File /r "C:\code\openrpa\OpenRPA.IE\bin\PrepInstaller\net462\OpenRPA.IE.resources.dll"
SectionEnd
Section "Chrome and Firefox (NM)" ; section 5
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NativeMessagingHost.exe"
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NativeMessagingHost.exe.config"
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NativeMessagingHost.pdb"
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NM.dll"
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NM.pdb"
  File /r "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\OpenRPA.NM.resources.dll"

  # SetOverwrite ifnewer
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\chromemanifest.json"
  File "C:\code\openrpa\OpenRPA.NM\bin\PrepInstaller\net462\ffmanifest.json"
  !insertmacro StrRep '$0' "$INSTDIR\OpenRPA.NativeMessagingHost.exe" '\' '\\'
  !insertmacro _ReplaceInFile "chromemanifest.json" "REPLACEPATH" $0
  !insertmacro _ReplaceInFile "ffmanifest.json" "REPLACEPATH" $0
SectionEnd
Section "Image recognition and OCR" ; section 6
  SetOutPath $INSTDIR\x64
  File /r "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\x64\*.*"
  SetOutPath $INSTDIR\x86
  File /r "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\x86\*.*"
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\Emgu.CV.UI.dll"
  File "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\Emgu.CV.World.dll"
  File "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\OpenRPA.Image.dll"
  File "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\OpenRPA.Image.pdb"
  File "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\ZedGraph.dll"
  File /r "C:\code\openrpa\OpenRPA.Image\bin\PrepInstaller\net462\OpenRPA.Image.resources.dll"
SectionEnd
Section "Generic Scripting support" ; section 7
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\OpenRPA.Script.dll"
  File "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\OpenRPA.Script.pdb"
  File "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\Python.Included.dll"
  File "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\Python.Runtime.NETStandard.dll"
  File "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\sharpAHK.dll"
  File /r "C:\code\openrpa\OpenRPA.Script\bin\PrepInstaller\net462\OpenRPA.Script.resources.dll"
SectionEnd
Section "AviRecorder" ; section 8
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\NAudio.dll"
  File "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\OpenRPA.AviRecorder.dll"
  File "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\OpenRPA.AviRecorder.pdb"
  File "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\SharpAvi.dll"
  File /r "C:\code\openrpa\OpenRPA.AviRecorder\bin\PrepInstaller\net462\OpenRPA.AviRecorder.resources.dll"
SectionEnd
Section "OpenFlowDB" ; section 9
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.OpenFlowDB\bin\PrepInstaller\net462\OpenRPA.OpenFlowDB.dll"
  File "C:\code\openrpa\OpenRPA.OpenFlowDB\bin\PrepInstaller\net462\OpenRPA.OpenFlowDB.pdb"
  File /r "C:\code\openrpa\OpenRPA.OpenFlowDB\bin\PrepInstaller\net462\OpenRPA.OpenFlowDB.resources.dll"
SectionEnd
Section "FileWatcher" ; section 10
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.FileWatcher\bin\PrepInstaller\net462\OpenRPA.FileWatcher.dll"
  File "C:\code\openrpa\OpenRPA.FileWatcher\bin\PrepInstaller\net462\OpenRPA.FileWatcher.pdb"
  File /r "C:\code\openrpa\OpenRPA.FileWatcher\bin\PrepInstaller\net462\OpenRPA.FileWatcher.resources.dll"
SectionEnd

Section "Java support" ; section 11
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.Java.dll"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.Java.dll.config"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.Java.pdb"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.JavaBridge.exe"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.JavaBridge.exe.config"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.JavaBridge.pdb"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\WindowsAccessBridgeInterop.dll"
  File "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\WindowsAccessBridgeInterop.pdb"
  File /r "C:\code\openrpa\OpenRPA.Java\bin\PrepInstaller\net462\OpenRPA.Java.resources.dll"
SectionEnd
Section "High Density robots using Remote Desktop" ; section 12
  SetOutPath $INSTDIR\x64
  File /r "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\x64\*.*"
  SetOutPath $INSTDIR\x86
  File /r "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\x86\*.*"
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\AxInterop.MSTSCLib.dll"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\FreeRDP.dll"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\MSTSCLib.dll"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDService.exe"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDService.exe.config"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDService.pdb"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDServicePlugin.dll"
  File "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDServicePlugin.pdb"
  ; File /r "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDService.resources.dll"
  File /r "C:\code\openrpa\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462\OpenRPA.RDServicePlugin.resources.dll"
SectionEnd
Section "Elis Rossum" ; section 13
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.Elis.Rossum\bin\PrepInstaller\net462\OpenRPA.Elis.Rossum.dll"
  File "C:\code\openrpa\OpenRPA.Elis.Rossum\bin\PrepInstaller\net462\OpenRPA.Elis.Rossum.pdb"
  File /r "C:\code\openrpa\OpenRPA.Elis.Rossum\bin\PrepInstaller\net462\OpenRPA.Elis.Rossum.resources.dll"
SectionEnd
Section "Process Discovery" ; section 14
  SetOutPath $INSTDIR
  File "C:\code\openrpa\OpenRPA.PDPlugin\bin\PrepInstaller\net462\OpenRPA.PDPlugin.dll"
  File "C:\code\openrpa\OpenRPA.PDPlugin\bin\PrepInstaller\net462\OpenRPA.PDPlugin.pdb"
SectionEnd
