@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo [INFO] NovaOryn IDE Run 0.1.38

set "NOVAORYN_IDE_ROOT=%~dp0"
set "NOVAORYN_SDK_ROOT=%~dp0SDK"

set "NOVAORYN_NODE=%~dp0.toolchain\Node\node.exe"
set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"
set "ELECTRON_MAIN=%~dp0applications\electron\lib\backend\electron-main.js"

if not exist "%NOVAORYN_NODE%" goto NEED_BUILD
if not exist "%NOVAORYN_NPM%" goto NEED_BUILD
if not exist "%NOVAORYN_PYTHON%" goto NEED_BUILD
if not exist "%ELECTRON_MAIN%" goto NEED_BUILD

goto CHECK_THEIA

:NEED_BUILD
echo [INFO] NovaOryn IDE is not fully prepared. Building it now...
call "%~dp0Build-NovaOrynIDE.bat"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" exit /b %RESULT%

:CHECK_THEIA
set "PATH=%~dp0.toolchain\Node;%~dp0.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

call "%NOVAORYN_NPM%" ls @theia/cli --workspace @novaoryn/ide-electron --depth=0 >nul 2>nul
if errorlevel 1 (
  echo [INFO] Eclipse Theia CLI package is missing. Rebuilding NovaOryn IDE...
  call "%~dp0Build-NovaOrynIDE.bat"
  set "RESULT=%errorlevel%"
  if not "%RESULT%"=="0" exit /b %RESULT%
)

if not exist "%ELECTRON_MAIN%" (
  echo [INFO] NovaOryn IDE Electron backend is missing. Rebuilding NovaOryn IDE...
  call "%~dp0Build-NovaOrynIDE.bat"
  set "RESULT=%errorlevel%"
  if not "%RESULT%"=="0" exit /b %RESULT%
)

echo [INFO] Starting NovaOryn IDE 0.1.38...
call "%NOVAORYN_NPM%" run start --workspace @novaoryn/ide-electron
set "RESULT=%errorlevel%"
exit /b %RESULT%
