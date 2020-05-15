@echo off
SET PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin
rem Lets focus on 64bit for now
IF NOT EXIST C:\code\openrpa\OpenRPA\bin\PrepInstaller\net462\x86 GOTO 472
del /s /q C:\code\openrpa\OpenRPA\bin\PrepInstaller\net462\x86\*.*
rmdir /s /q C:\code\openrpa\OpenRPA\bin\PrepInstaller\net462\x86
:472
IF NOT EXIST C:\code\openrpa\OpenRPA\bin\PrepInstaller\net472\x86 GOTO HARVEST
del /s /q C:\code\openrpa\OpenRPA\bin\PrepInstaller\net472\x86\*.*
rmdir /s /q C:\code\openrpa\OpenRPA\bin\PrepInstaller\net472\x86

:HARVEST
rem use -gg for generate guid, use -ag to remove guid
rem heat dir "..\OpenRPA\bin\PrepInstaller\net462" -gg -cg MainComponentFiles -dr INSTALLDIR -scom -sreg -srd -sfrag -var wix.MainSource -out mainfiles2.wxs -t HeatTransform.xslt -nologo
rem heat dir "..\OpenRPA.Office\bin\PrepInstaller\net462" -gg -cg OfficeComponents -dr INSTALLDIR -scom -sreg -srd -sfrag -var wix.OfficeSource -out officefiles2.wxs -t HeatTransform2.xslt -nologo


rem heat dir "..\OpenRPA\bin\PrepInstaller\net462" -gg -cg MainComponentFiles -dr INSTALLDIR -scom -sreg -srd -sfrag -var wix.MainSource -out mainfiles2.wxs -t HeatTransform.xslt -nologo


