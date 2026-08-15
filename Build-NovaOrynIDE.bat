@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "BOOTSTRAP=%~dp0Install-NovaOrynIDEToolchain.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing toolchain bootstrap script: %BOOTSTRAP%
  exit /b 1
)

echo [INFO] Verifying NovaOryn IDE 0.0.10 build toolchain...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%BOOTSTRAP%"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE toolchain verification failed with exit code %RESULT%.
  exit /b %RESULT%
)

rem Resolve the repository-pinned tools directly.  Do not call a generated
rem environment .cmd file: build control flow must remain inside this script.
set "NOVAORYN_NODE=%~dp0.toolchain\Node\node.exe"
set "NOVAORYN_NPM=%~dp0.toolchain\Node\npm.cmd"
set "NOVAORYN_PYTHON=%~dp0.toolchain\Python\python.exe"

if not exist "%NOVAORYN_NODE%" (
  echo [FAIL] Pinned Node.js is unavailable: %NOVAORYN_NODE%
  exit /b 1
)
if not exist "%NOVAORYN_NPM%" (
  echo [FAIL] Pinned npm is unavailable: %NOVAORYN_NPM%
  exit /b 1
)
if not exist "%NOVAORYN_PYTHON%" (
  echo [FAIL] Pinned Python is unavailable: %NOVAORYN_PYTHON%
  exit /b 1
)

set "PATH=%~dp0.toolchain\Node;%~dp0.toolchain\Python;%PATH%"
set "npm_config_python=%NOVAORYN_PYTHON%"
set "PYTHON=%NOVAORYN_PYTHON%"
set "NODE_ENV=development"
set "npm_config_omit="
set "NPM_CONFIG_OMIT="

echo [INFO] Node.js : %NOVAORYN_NODE%
"%NOVAORYN_NODE%" --version
if errorlevel 1 exit /b %errorlevel%
echo [INFO] npm     : %NOVAORYN_NPM%
call "%NOVAORYN_NPM%" --version
if errorlevel 1 exit /b %errorlevel%
echo [INFO] Python  : %NOVAORYN_PYTHON%
"%NOVAORYN_PYTHON%" --version
if errorlevel 1 exit /b %errorlevel%

echo [INFO] Verifying NovaOryn IDE dependency compatibility...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Validate-NovaOrynIDEDependencies.ps1"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE dependency compatibility verification failed.
  exit /b %RESULT%
)

if exist "%~dp0package-lock.json" (
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$p='%~dp0package-lock.json'; try { $j=Get-Content -LiteralPath $p -Raw ^| ConvertFrom-Json; $v=[string]$j.version; if ($v -and $v -ne '0.0.10') { Write-Host '[INFO] Removing stale package-lock.json from NovaOryn IDE' $v; Remove-Item -LiteralPath $p -Force } } catch { Write-Host '[INFO] Removing unreadable package-lock.json so npm can regenerate it.'; Remove-Item -LiteralPath $p -Force }"
  if errorlevel 1 (
    echo [FAIL] Could not validate the existing package-lock.json.
    exit /b 1
  )
)

echo [INFO] Installing NovaOryn IDE JavaScript dependencies, including development tools...
call "%NOVAORYN_NPM%" install --include=dev --workspaces
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] npm dependency installation failed with exit code %RESULT%.
  exit /b %RESULT%
)

echo [INFO] Verifying Eclipse Theia CLI package...
call "%NOVAORYN_NPM%" ls @theia/cli --workspace @novaoryn/ide-electron --depth=0
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] @theia/cli is not installed for @novaoryn/ide-electron.
  echo [INFO] npm omit setting:
  call "%NOVAORYN_NPM%" config get omit
  exit /b 1
)
echo [ OK ] Eclipse Theia CLI package is installed.

echo [INFO] Verifying Windows CA certificate module required by the Theia Node bundle...
call "%NOVAORYN_NPM%" ls @vscode/windows-ca-certs --workspace @novaoryn/ide-electron --depth=0
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] @vscode/windows-ca-certs is not installed for @novaoryn/ide-electron.
  exit /b 1
)
echo [ OK ] Windows CA certificate module is installed.

echo [INFO] Verifying TypeScript compiler for the NovaOryn Theia extension...
call "%NOVAORYN_NPM%" ls typescript --workspace @novaoryn/ide-extension --depth=0
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] TypeScript is not installed for @novaoryn/ide-extension.
  exit /b 1
)
echo [ OK ] TypeScript compiler is installed.

echo [INFO] Building NovaOryn IDE 0.0.10...
call "%NOVAORYN_NPM%" run build
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE build failed with exit code %RESULT%.
  exit /b %RESULT%
)

if not exist "%~dp0packages\novaoryn-ide\lib\browser\novaoryn-frontend-module.js" (
  echo [FAIL] The build reported success but the NovaOryn frontend extension module was not generated.
  exit /b 1
)
if not exist "%~dp0packages\novaoryn-ide\lib\node\novaoryn-backend-module.js" (
  echo [FAIL] The build reported success but the NovaOryn backend extension module was not generated.
  exit /b 1
)
if not exist "%~dp0packages\novaoryn-ide\lib\browser\style\novaoryn.css" (
  echo [FAIL] The build reported success but the NovaOryn extension stylesheet was not copied.
  exit /b 1
)
if not exist "%~dp0applications\electron\lib\backend\electron-main.js" (
  echo [FAIL] The build reported success but the Electron backend was not generated.
  exit /b 1
)

echo [ OK ] NovaOryn IDE 0.0.10 build completed.
exit /b 0
