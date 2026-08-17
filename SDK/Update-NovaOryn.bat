@echo off
setlocal EnableExtensions
set "BOOTSTRAP=%~dp0Bootstrap-Update-NovaOryn.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing Bootstrap-Update-NovaOryn.ps1 beside this batch file.
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%BOOTSTRAP%" %*
set "EXITCODE=%ERRORLEVEL%"
endlocal & exit /b %EXITCODE%
