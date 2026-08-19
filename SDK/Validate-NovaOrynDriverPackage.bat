@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Validate-NovaOrynDriverPackage.ps1" %*
exit /b %ERRORLEVEL%
