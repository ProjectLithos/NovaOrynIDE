@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

rem NovaOryn IDE release contract: 0.18.0. VERSION line 1 is authoritative.
rem Resolve VERSION through a one-line scratch file so the manifest body can never become CMD input.
set "NOVAORYN_VERSION_SCRATCH=%~dp0.toolchain\novaoryn-ide-version.txt"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Resolve-NovaOrynIDEVersion.ps1" -OutputPath "%NOVAORYN_VERSION_SCRATCH%"
if errorlevel 1 exit /b 1
set /p NOVAORYN_IDE_VERSION=<"%NOVAORYN_VERSION_SCRATCH%"
if not defined NOVAORYN_IDE_VERSION (
  echo [FAIL] Resolved NovaOryn IDE version is empty.
  exit /b 1
)
echo [INFO] NovaOryn IDE Run %NOVAORYN_IDE_VERSION%

set "NOVAORYN_IDE_ROOT=%~dp0"
set "NOVAORYN_SDK_ROOT=%~dp0SDK"
set "NOVAORYN_NODE=%~dp0.toolchain\Node\node.exe"
set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_NPM_PREFIX=%~dp0.toolchain\NpmWorkspace"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"
set "NOVAORYN_GENERATED_BUILD_VERSION=%~dp0applications\electron\lib\.novaoryn-build-version"
set "NOVAORYN_GENERATED_BUILD_STATE=%~dp0applications\electron\lib\.novaoryn-build-state.json"
set "ELECTRON_MAIN=%~dp0applications\electron\lib\backend\electron-main.js"

rem Run is intentionally launch-only. It never invokes Build-NovaOrynIDE.bat.
rem A missing/stale prerequisite is reported so Build and Run remain distinct operations.
if not exist "%NOVAORYN_NODE%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM%" goto NOT_BUILT
if not exist "%NOVAORYN_PYTHON%" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\package.json" goto NOT_BUILT
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules" goto NOT_BUILT
if not exist "%NOVAORYN_GENERATED_BUILD_VERSION%" goto NOT_BUILT
if not exist "%NOVAORYN_GENERATED_BUILD_STATE%" goto NOT_BUILT
if not exist "%ELECTRON_MAIN%" goto NOT_BUILT

set "NOVAORYN_BUILDSTATE_TOOL=%~dp0Scripts\Manage-NovaOrynIDEBuildState.ps1"
if not exist "%NOVAORYN_BUILDSTATE_TOOL%" goto NOT_BUILT
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_BUILDSTATE_TOOL%" -Action Validate
if errorlevel 1 goto STALE_BUILD

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
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Validate-NovaOrynIDERuntimePackages.ps1"
if errorlevel 1 goto INCOMPLETE_RUNTIME

echo [ OK ] Existing NovaOryn IDE %NOVAORYN_IDE_VERSION% build is ready.
echo [INFO] Starting NovaOryn IDE %NOVAORYN_IDE_VERSION%...
call "%NOVAORYN_NPM%" --prefix "%NOVAORYN_NPM_PREFIX%" run start --workspace @novaoryn/ide-electron
set "RESULT=!errorlevel!"
exit /b !RESULT!

:INCOMPLETE_RUNTIME
echo [FAIL] The built IDE runtime is incomplete or does not match Theia 1.74.0 / Electron 42.3.0.
echo [INFO] Run Build-NovaOrynIDE.bat, then run this launcher again.
exit /b 1

:STALE_BUILD
echo [FAIL] The existing NovaOryn IDE build is not version %NOVAORYN_IDE_VERSION% or its generated-build marker is stale.
echo [INFO] Run Build-NovaOrynIDE.bat once, then use Run-NovaOrynIDE.bat to launch it.
exit /b 1

:NOT_BUILT
echo [FAIL] NovaOryn IDE %NOVAORYN_IDE_VERSION% has not been built completely yet.
echo [INFO] Run Build-NovaOrynIDE.bat once. This Run script will not build the IDE automatically.
exit /b 1
