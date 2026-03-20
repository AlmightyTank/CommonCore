@echo off
setlocal

REM %1 = ProjectDir
REM %2 = TargetDir
REM %3 = AssemblyName

set "TARGETDIR=%~2"
set "ASSEMBLY=%~3"

REM Change this to your SPT install
set "SPTROOT=C:\SPT\SPT"

set "MODDIR=%SPTROOT%\user\mods\CommonCore"

echo ===============================
echo Deploying %ASSEMBLY%
echo ===============================

if not exist "%MODDIR%" (
    mkdir "%MODDIR%"
)

copy /Y "%TARGETDIR%%ASSEMBLY%.dll" "%MODDIR%\"

echo Done.
exit /b 0