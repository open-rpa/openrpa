@echo off
rem "C:\Program Files (x86)\WiX Toolset v3.11\bin\heat" dir "c:\code\OpenRPA\OpenRPA\bin\PrepInstaller\net462" -gg -sfrag -template:fragment -out C:\code\openrpa\OpenRPASetupProject\mainfiles.wxs

SET PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin


rem heat dir "c:\code\OpenRPA\OpenRPA\bin\PrepInstaller\net462" -gg -sfrag -template:fragment -out C:\code\openrpa\OpenRPASetupProject\mainfiles.wxs
rem heat dir "c:\code\OpenRPA\OpenRPA\bin\PrepInstaller\net462" -gg -sfrag -template:fragment -out C:\code\openrpa\OpenRPASetupProject\mainfiles.wxs

rem heat dir "c:\code\OpenRPA\OpenRPA\bin\PrepInstaller\net462" -gg -sfrag -template:fragment -out C:\code\openrpa\OpenRPASetupProject\mainfiles.wxs -cg MainComponentFiles



heat dir "..\OpenRPA\bin\PrepInstaller\net462" -nologo -sw -gg -sfrag -svb6 -template fragment -out mainfiles.wxs -cg MainComponentFiles -dr INSTALLDIR -var wix.MySource -platform x64 -t HeatTransform.xslt

heat dir "..\OpenRPA.RDServicePlugin\bin\PrepInstaller\net462" -nologo -sw -gg -sfrag -svb6 -template fragment -out mainfiles.wxs -cg MainComponentFiles -dr INSTALLDIR -var wix.MySource -platform x64 -t HeatTransform.xslt



