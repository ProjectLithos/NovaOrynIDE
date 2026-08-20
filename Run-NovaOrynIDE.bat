@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo [INFO] NovaOryn IDE Run 0.11.8

set "NOVAORYN_IDE_ROOT=%~dp0"
set "NOVAORYN_SDK_ROOT=%~dp0SDK"
set "NOVAORYN_NODE=%~dp0.toolchain\Node\node.exe"
set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_NPM_PREFIX=%~dp0.toolchain\NpmWorkspace"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"
set "NOVAORYN_BUILDSTATE=%~dp0.toolchain\NovaOrynIDE-BuildState.json"
set "ELECTRON_MAIN=%~dp0applications\electron\lib\backend\electron-main.js"

rem Run is intentionally launch-only. It never invokes Build-NovaOrynIDE.bat.
rem A missing/stale prerequisite is reported so Build and Run remain distinct operations.
if not exist "%NOVAORYN_NODE%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM%" goto NOT_BUILT
if not exist "%NOVAORYN_PYTHON%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\package.json" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules" goto NOT_BUILT
if not exist "%NOVAORYN_BUILDSTATE%" goto NOT_BUILT
if not exist "%ELECTRON_MAIN%" goto NOT_BUILT

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$p='%NOVAORYN_BUILDSTATE%'; try { $j=Get-Content -LiteralPath $p -Raw | ConvertFrom-Json; if ([string]$j.novaOrynIdeVersion -ne '0.11.8') { exit 2 }; exit 0 } catch { exit 3 }"
if errorlevel 1 goto STALE_BUILD

set "PATH=%~dp0.toolchain\Node;%~dp0.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

echo [INFO] Verifying already-built Theia runtime...
pushd "%NOVAORYN_NPM_PREFIX%" >nul
call "%NOVAORYN_NPM%" ls @theia/cli --workspace @novaoryn/ide-electron --depth=0 >nul 2>nul
set "THEIA_CHECK=!errorlevel!"
popd >nul
if not "!THEIA_CHECK!"=="0" (
  echo [FAIL] The built IDE dependency tree is incomplete.
  echo [INFO] Run Build-NovaOrynIDE.bat, then run this launcher again.
  exit /b 1
)

echo [ OK ] Existing NovaOryn IDE 0.11.8 build is ready.
echo [INFO] Starting NovaOryn IDE 0.11.8...
pushd "%NOVAORYN_NPM_PREFIX%" >nul
call "%NOVAORYN_NPM%" run start --workspace @novaoryn/ide-electron
set "RESULT=!errorlevel!"
popd >nul
exit /b !RESULT!

:STALE_BUILD
echo [FAIL] The existing NovaOryn IDE build is not version 0.11.8 or its build-state marker is invalid.
echo [INFO] Run Build-NovaOrynIDE.bat once, then use Run-NovaOrynIDE.bat to launch it.
exit /b 1

:NOT_BUILT
echo [FAIL] NovaOryn IDE 0.11.8 has not been built completely yet.
echo [INFO] Run Build-NovaOrynIDE.bat once. This Run script will not build the IDE automatically.
exit /b 1
