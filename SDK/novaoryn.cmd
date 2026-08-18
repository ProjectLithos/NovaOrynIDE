@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0novaoryn.ps1" %*
exit /b %ERRORLEVEL%
