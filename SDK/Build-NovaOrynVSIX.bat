@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-NovaOrynVSIX.ps1" %*
exit /b %ERRORLEVEL%
