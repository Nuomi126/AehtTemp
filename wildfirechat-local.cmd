@echo off
setlocal
set "SCRIPT=%~dp0wildfirechat-local.ps1"
if "%~1"=="" (
  set "ACTION=Verify"
) else (
  set "ACTION=%~1"
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -Action "%ACTION%"
exit /b %ERRORLEVEL%
