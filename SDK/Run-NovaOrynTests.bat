@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-NovaOrynTests.ps1" %*
exit /b %ERRORLEVEL%
