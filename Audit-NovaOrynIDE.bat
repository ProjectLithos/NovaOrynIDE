@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "BOOTSTRAP=%~dp0Install-NovaOrynIDEToolchain.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing toolchain bootstrap script: %BOOTSTRAP%
  exit /b 1
)

echo [INFO] Verifying NovaOryn IDE 0.1.52 audit toolchain...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%BOOTSTRAP%"
if errorlevel 1 exit /b %errorlevel%

set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"
set "PATH=%~dp0.toolchain\Node;%~dp0.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

if not exist "%~dp0node_modules" (
  echo [INFO] Dependencies are not installed. Installing the pinned workspace graph first...
  call "%NOVAORYN_NPM%" install --include=dev --workspaces
  if errorlevel 1 exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Audit-NovaOrynIDE.ps1"
exit /b %errorlevel%
