@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo [INFO] NovaOryn IDE Run 0.11.15

set "NOVAORYN_IDE_ROOT=%~dp0"
set "NOVAORYN_SDK_ROOT=%~dp0SDK"
set "NOVAORYN_NODE=%~dp0.toolchain\Node\node.exe"
set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_NPM_PREFIX=%~dp0.toolchain\NpmWorkspace"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"
set "NOVAORYN_GENERATED_BUILD_VERSION=%~dp0applications\electron\lib\.novaoryn-build-version"
set "ELECTRON_MAIN=%~dp0applications\electron\lib\backend\electron-main.js"

rem Run is intentionally launch-only. It never invokes Build-NovaOrynIDE.bat.
rem A missing/stale prerequisite is reported so Build and Run remain distinct operations.
if not exist "%NOVAORYN_NODE%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM%" goto NOT_BUILT
if not exist "%NOVAORYN_PYTHON%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\package.json" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules" goto NOT_BUILT
if not exist "%NOVAORYN_GENERATED_BUILD_VERSION%" goto NOT_BUILT
if not exist "%ELECTRON_MAIN%" goto NOT_BUILT

set /p NOVAORYN_BUILT_VERSION=<"%NOVAORYN_GENERATED_BUILD_VERSION%"
if not "%NOVAORYN_BUILT_VERSION%"=="0.11.15" goto STALE_BUILD

set "PATH=%~dp0.toolchain\Node;%~dp0.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

echo [INFO] Verifying already-built Theia runtime...
set "THEIA_CLI_PACKAGE=%NOVAORYN_NPM_PREFIX%\node_modules\@theia\cli\package.json"
set "THEIA_ELECTRON_PACKAGE=%NOVAORYN_NPM_PREFIX%\node_modules\@theia\electron\package.json"
set "ELECTRON_PACKAGE=%NOVAORYN_NPM_PREFIX%\node_modules\electron\package.json"
if not exist "%THEIA_CLI_PACKAGE%" goto INCOMPLETE_RUNTIME
if not exist "%THEIA_ELECTRON_PACKAGE%" goto INCOMPLETE_RUNTIME
if not exist "%ELECTRON_PACKAGE%" goto INCOMPLETE_RUNTIME
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$items=@(@('%THEIA_CLI_PACKAGE%','1.74.0'),@('%THEIA_ELECTRON_PACKAGE%','1.74.0'),@('%ELECTRON_PACKAGE%','42.3.0')); foreach($i in $items){ try { $j=Get-Content -LiteralPath $i[0] -Raw | ConvertFrom-Json; if([string]$j.version -ne [string]$i[1]){ exit 2 } } catch { exit 3 } }; exit 0"
if errorlevel 1 goto INCOMPLETE_RUNTIME

echo [ OK ] Existing NovaOryn IDE 0.11.15 build is ready.
echo [INFO] Starting NovaOryn IDE 0.11.15...
pushd "%NOVAORYN_NPM_PREFIX%" >nul
call "%NOVAORYN_NPM%" run start --workspace @novaoryn/ide-electron
set "RESULT=!errorlevel!"
popd >nul
exit /b !RESULT!

:INCOMPLETE_RUNTIME
echo [FAIL] The built IDE runtime is incomplete or does not match Theia 1.74.0 / Electron 42.3.0.
echo [INFO] Run Build-NovaOrynIDE.bat, then run this launcher again.
exit /b 1

:STALE_BUILD
echo [FAIL] The existing NovaOryn IDE build is not version 0.11.15 or its generated-build marker is stale.
echo [INFO] Run Build-NovaOrynIDE.bat once, then use Run-NovaOrynIDE.bat to launch it.
exit /b 1

:NOT_BUILT
echo [FAIL] NovaOryn IDE 0.11.15 has not been built completely yet.
echo [INFO] Run Build-NovaOrynIDE.bat once. This Run script will not build the IDE automatically.
exit /b 1
