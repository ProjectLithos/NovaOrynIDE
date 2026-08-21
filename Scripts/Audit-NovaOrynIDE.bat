@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "NOVAORYN_IDE_ROOT=%~dp0..\"
cd /d "%NOVAORYN_IDE_ROOT%"

set "BOOTSTRAP=%~dp0Install-NovaOrynIDEToolchain.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing toolchain bootstrap script: %BOOTSTRAP%
  exit /b 1
)

echo [INFO] Verifying NovaOryn IDE 0.15.0 audit toolchain...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%BOOTSTRAP%"
if errorlevel 1 exit /b %errorlevel%

set "NOVAORYN_NPM=%NOVAORYN_IDE_ROOT%.toolchain\Node\npm.cmd"
set "NOVAORYN_PYTHON=%NOVAORYN_IDE_ROOT%.toolchain\Python\python.exe"
set "NOVAORYN_NPM_PREFIX=%NOVAORYN_IDE_ROOT%.toolchain\NpmWorkspace"
set "PATH=%NOVAORYN_IDE_ROOT%.toolchain\Node;%NOVAORYN_IDE_ROOT%.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

if not exist "%NOVAORYN_NPM_PREFIX%\package.json" (
  echo [FAIL] The staged npm workspace is missing.
  echo [INFO] Run Build-NovaOrynIDE.bat once before running the standalone audit.
  exit /b 1
)
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules" (
  echo [INFO] Dependencies are not installed. Installing the pinned workspace graph first...
  pushd "%NOVAORYN_NPM_PREFIX%" >nul
  call "%NOVAORYN_NPM%" install --include=dev --workspaces
  set "RESULT=!errorlevel!"
  popd >nul
  if not "!RESULT!"=="0" exit /b !RESULT!
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Audit-NovaOrynIDE.ps1"
exit /b %errorlevel%
