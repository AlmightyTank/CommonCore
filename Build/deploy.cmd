@echo off
setlocal

REM %1 = ProjectDir
REM %2 = TargetDir
REM %3 = AssemblyName

set "PROJECTDIR=%~1"
set "TARGETDIR=%~2"
set "ASSEMBLY=%~3"

REM Change this to your SPT install
set "SPTROOT=C:\SPT\SPT"

set "MODDIR=%SPTROOT%\user\mods\CommonLibExtended"

echo ===============================
echo Deploying %ASSEMBLY%
echo ===============================

if not exist "%MODDIR%" (
    mkdir "%MODDIR%"
)

echo Copying DLL...
copy /Y "%TARGETDIR%%ASSEMBLY%.dll" "%MODDIR%\" >nul

echo Copying config.json...
if exist "%PROJECTDIR%config.json" (
    copy /Y "%PROJECTDIR%config.json" "%MODDIR%\" >nul
) else (
    echo config.json not found, skipping...
)

echo Done.
exit /b 0