@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-NovaOrynVSIX.ps1" %*
exit /b %ERRORLEVEL%
