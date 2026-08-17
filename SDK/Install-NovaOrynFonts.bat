@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-NovaOrynFonts.ps1" %*
exit /b %ERRORLEVEL%
