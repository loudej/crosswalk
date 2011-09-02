@echo off

echo This is to make testing easier on a x64 dev server


echo ==========
echo Symbolic links for x64 w3wp.exe

echo ----------
del    %windir%\system32\inetsrv\Crosswalk.dll
rem mklink %windir%\system32\inetsrv\Crosswalk.dll %~dp0Debug\Crosswalk.dll

echo ----------
del    %windir%\system32\inetsrv\CrosswalkModule.dll
mklink %windir%\system32\inetsrv\CrosswalkModule.dll %~dp0src\CrosswalkModule\bin\x64\Debug\CrosswalkModule.dll


echo ==========
echo Symbolic links for x86 w3wp.exe

echo ----------
del    %windir%\syswow64\inetsrv\Crosswalk.dll
rem mklink %windir%\syswow64\inetsrv\Crosswalk.dll %~dp0Debug\Crosswalk.dll

echo ----------
del    %windir%\syswow64\inetsrv\CrosswalkModule.dll
mklink %windir%\syswow64\inetsrv\CrosswalkModule.dll %~dp0src\CrosswalkModule\bin\Debug\CrosswalkModule.dll


echo ==========
echo Symbolic links for x86 iisexpress.exe

echo ----------
del    "%ProgramFiles(x86)%\iis express\Crosswalk.dll"
rem mklink "%ProgramFiles(x86)%\iis express\Crosswalk.dll" %~dp0Debug\Crosswalk.dll

echo ----------
del    "%ProgramFiles(x86)%\iis express\CrosswalkModule.dll"
mklink "%ProgramFiles(x86)%\iis express\CrosswalkModule.dll" %~dp0src\CrosswalkModule\bin\Debug\CrosswalkModule.dll
