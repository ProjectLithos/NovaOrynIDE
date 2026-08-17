@echo off
setlocal
set "ROOT=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%ROOT%Validate-NovaOryn.ps1" %*
exit /b %ERRORLEVEL%
