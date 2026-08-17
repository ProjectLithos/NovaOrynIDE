@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo [INFO] NovaOryn IDE Build 0.1.52

set "NOVAORYN_IDE_ROOT=%~dp0"
set "NOVAORYN_SDK_ROOT=%~dp0SDK"
set "NOVAORYN_EMBEDDED_SDK=1"
echo [INFO] Embedded SDK mode: %NOVAORYN_EMBEDDED_SDK%
if not exist "%NOVAORYN_SDK_ROOT%\Build-NovaOryn.bat" (
  echo [FAIL] Bundled NovaOryn SDK was not found at %NOVAORYN_SDK_ROOT%.
  echo [INFO] Use the NovaOryn IDE FullSource package, which contains SDK\.
  exit /b 1
)
echo [ OK ] Bundled NovaOryn SDK: %NOVAORYN_SDK_ROOT%

rem The IDE build only needs the SDK source tree.  Do NOT install or verify the
rem SDK's .NET/LLVM/ILC/QEMU toolchain here: that can take a long time and is
rem only required when an OS/kernel is actually built or run.
echo [INFO] Bundled NovaOryn SDK source verified.
if exist "%NOVAORYN_SDK_ROOT%\.toolchain\DotNet\dotnet.exe" (
  echo [ OK ] Existing bundled SDK .NET toolchain detected; it will be reused by OS builds.
) else (
  echo [INFO] Bundled SDK toolchain is not installed yet.
  echo [INFO] It will be installed/verified when the SDK is first used to build or run a NovaOryn OS.
)

set "BOOTSTRAP=%~dp0Install-NovaOrynIDEToolchain.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing toolchain bootstrap script: %BOOTSTRAP%
  exit /b 1
)

echo [INFO] Verifying NovaOryn IDE 0.1.52 build toolchain...
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
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$p='%~dp0package-lock.json'; try { $j=Get-Content -LiteralPath $p -Raw ^| ConvertFrom-Json; $v=[string]$j.version; if ($v -and $v -ne '0.1.52') { Write-Host '[INFO] Removing stale package-lock.json from NovaOryn IDE' $v; Remove-Item -LiteralPath $p -Force } } catch { Write-Host '[INFO] Removing unreadable package-lock.json so npm can regenerate it.'; Remove-Item -LiteralPath $p -Force }"
  if errorlevel 1 (
    echo [FAIL] Could not validate the existing package-lock.json.
    exit /b 1
  )
)

rem npm maintains a hidden lockfile inside node_modules.  After a framework
rem transition it can preserve an old dependency graph even when package-lock.json
rem has been removed.  Treat the installed tree as disposable whenever the recorded
rem Theia/Electron pair does not match the pinned pair.
set "NOVAORYN_BUILDSTATE=%~dp0.toolchain\NovaOrynIDE-BuildState.json"
set "NOVAORYN_BROWSER_MODULES=%~dp0.browser_modules"
set "NOVAORYN_NODE_MODULES=%~dp0node_modules"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$state='%NOVAORYN_BUILDSTATE%'; $cache='%NOVAORYN_BROWSER_MODULES%'; $modules='%NOVAORYN_NODE_MODULES%'; $wantedTheia='1.74.0'; $wantedElectron='42.3.0'; $stale=$false; if (Test-Path -LiteralPath $state) { try { $j=Get-Content -LiteralPath $state -Raw | ConvertFrom-Json; if ([string]$j.theiaVersion -ne $wantedTheia -or [string]$j.electronVersion -ne $wantedElectron) { $stale=$true } } catch { $stale=$true } } elseif ((Test-Path -LiteralPath $modules) -or (Test-Path -LiteralPath $cache)) { $stale=$true }; if ($stale -and (Test-Path -LiteralPath $modules)) { Write-Host '[INFO] Removing stale npm dependency tree from an earlier Theia/Electron pair.'; Remove-Item -LiteralPath $modules -Recurse -Force -ErrorAction Stop }; if ($stale -and (Test-Path -LiteralPath $cache)) { Write-Host '[INFO] Removing stale Theia native-module cache from an earlier dependency set.'; Remove-Item -LiteralPath $cache -Recurse -Force -ErrorAction Stop }; if ($stale) { foreach ($rel in @('applications\electron\lib','applications\electron\src-gen','applications\electron\gen-webpack.config.js','applications\electron\esbuild.mjs')) { $x=Join-Path '%~dp0' $rel; if (Test-Path -LiteralPath $x) { Remove-Item -LiteralPath $x -Recurse -Force -ErrorAction Stop } }; $lock=Join-Path '%~dp0' 'package-lock.json'; if (Test-Path -LiteralPath $lock) { Remove-Item -LiteralPath $lock -Force -ErrorAction Stop } }"
if errorlevel 1 (
  echo [FAIL] Could not invalidate stale npm/Theia dependency state.
  exit /b 1
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

echo [INFO] Verifying installed Theia/Electron runtime versions from the Electron workspace...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEInstalledDependencies.cjs"
if errorlevel 1 (
  echo [WARN] Installed dependency tree does not match the NovaOryn 0.1.52 pins.
  echo [INFO] Performing one clean dependency reinstall from the checked package manifests...
  if exist "%~dp0node_modules" rmdir /s /q "%~dp0node_modules"
  if exist "%~dp0package-lock.json" del /f /q "%~dp0package-lock.json"
  if exist "%~dp0.browser_modules" rmdir /s /q "%~dp0.browser_modules"
  call "%NOVAORYN_NPM%" install --include=dev --workspaces
  set "RESULT=!errorlevel!"
  if not "!RESULT!"=="0" (
    echo [FAIL] Clean npm dependency reinstall failed with exit code !RESULT!.
    exit /b !RESULT!
  )
  "%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEInstalledDependencies.cjs"
  if errorlevel 1 (
    echo [FAIL] Clean reinstall still does not match the NovaOryn 0.1.52 dependency pins.
    exit /b 2
  )
)

echo [INFO] Verifying Windows CA certificate module required by the Theia Node bundle...
call "%NOVAORYN_NPM%" ls @vscode/windows-ca-certs --workspace @novaoryn/ide-electron --depth=0
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] @vscode/windows-ca-certs is not installed for @novaoryn/ide-electron.
  exit /b 1
)
echo [ OK ] Windows CA certificate module is installed.

echo [INFO] Verifying root Theia CLI dependency surface...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDETheiaCliDependencies.cjs"
if errorlevel 1 (
  echo [FAIL] Root Theia CLI dependency verification failed.
  exit /b 1
)

echo [INFO] Running NovaOryn IDE production security gate...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Audit-NovaOrynIDE.ps1" -GateProduction
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE production security gate failed.
  exit /b %RESULT%
)

echo [INFO] Verifying TypeScript compiler for the NovaOryn Theia extension...
call "%NOVAORYN_NPM%" ls typescript --workspace @novaoryn/ide-extension --depth=0
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] TypeScript is not installed for @novaoryn/ide-extension.
  exit /b 1
)
echo [ OK ] TypeScript compiler is installed.

if not exist "%~dp0applications\electron\splash\splash.html" (
  echo [FAIL] NovaOryn IDE splash screen HTML is missing.
  exit /b 1
)
if not exist "%~dp0applications\electron\splash\novaoryn-ide-splash.png" (
  echo [FAIL] NovaOryn IDE splash screen artwork is missing.
  exit /b 1
)
echo [ OK ] NovaOryn IDE splash screen assets are present.

if not exist "%~dp0applications\electron\resources\novaoryn-ide.ico" (
  echo [FAIL] NovaOryn IDE application icon is missing.
  exit /b 1
)
echo [ OK ] NovaOryn IDE application icon is present.

echo [INFO] Verifying authoritative NovaOryn configuration generator...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEAuthoritativeConfiguration.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn authoritative configuration verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying existing NovaOryn OS reconfiguration contract...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEReconfiguration.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn OS reconfiguration verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying system theme and syntax-highlighting contract...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEThemeAndSyntax.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE theme/syntax verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying build-owned GitHub source publishing contract...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEGitPublish.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE GitHub publishing verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying debugger breakpoint integration...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEDebugBreakpoints.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE debugger breakpoint verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying exact NativeAOT source-debug integration...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEBundledSdk.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE bundled-SDK verification failed.
  exit /b %RESULT%
)

"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDESourceDebug.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE source-debug verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying conditional breakpoints and Watch expressions...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEDebugExpressions.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE conditional-breakpoint/Watch verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying Memory viewer and named NativeAOT locals/arguments...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEMemoryLocals.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE Memory/locals verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying CPU/thread/process contexts, x64 call-stack unwinding, and NovaOryn application icon...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEExecutionUnwind.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE execution-context/x64-unwind/icon verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying page-table, heap and crash-dump debugging...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEPageHeapCrash.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE page-table/heap/crash-dump verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying professional OS Dashboard, Kernel Console, Hardware Tree and Test Explorer...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEProfessionalTools.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE professional OS tools verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying kernel tracing, boot analyser and performance profiler...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDETracingProfiler.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE tracing/profiler verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying bundled NovaOryn SDK API/ABI contract...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0SDK\Verify-NovaOrynSdkContract.ps1"
if errorlevel 1 ( echo [FAIL] Bundled NovaOryn SDK contract verification failed. & exit /b 1 )
echo [INFO] Verifying Driver Development Centre...
"%NOVAORYN_NODE%" "%~dp0Verify-NovaOrynIDEDriverCentre.cjs"
if errorlevel 1 (
  echo [FAIL] Driver Development Centre verification failed.
  exit /b 1
)
echo [INFO] Building NovaOryn IDE 0.1.52...
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

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$o=[ordered]@{ novaOrynIdeVersion='0.1.52'; theiaVersion='1.74.0'; electronVersion='42.3.0'; generatedUtc=(Get-Date).ToUniversalTime().ToString('o') }; $o | ConvertTo-Json | Set-Content -LiteralPath '%NOVAORYN_BUILDSTATE%' -Encoding UTF8"
if errorlevel 1 (
  echo [WARN] Build succeeded but NovaOryn could not record the dependency build-state marker.
)

echo [ OK ] NovaOryn IDE 0.1.52 build completed.

echo [INFO] Publishing NovaOryn IDE source to GitHub...
set "NOVAORYN_GIT_REMOTE=https://github.com/ProjectLithos/NovaOrynIDE.git"
set "NOVAORYN_GIT_BRANCH=main"

where git.exe >nul 2>&1
if errorlevel 1 (
  echo [FAIL] Git for Windows was not found on PATH.
  echo [INFO] Install Git for Windows, then run Build-NovaOrynIDE.bat again.
  exit /b 1
)

rem The bundled SDK is source inside the IDE repository, not a nested Git repository.
rem If it was previously used as a standalone checkout, remove only its Git metadata
rem so git add records the SDK files themselves rather than an embedded repository.
if exist "%NOVAORYN_SDK_ROOT%\.git" (
  echo [INFO] Removing nested SDK Git metadata so SDK source is committed with the IDE.
  rmdir /s /q "%NOVAORYN_SDK_ROOT%\.git"
  if exist "%NOVAORYN_SDK_ROOT%\.git" (
    echo [FAIL] Could not remove nested SDK Git metadata: %NOVAORYN_SDK_ROOT%\.git
    exit /b 1
  )
)

if not exist "%~dp0.git" (
  echo [INFO] Initialising NovaOrynIDE Git repository.
  git init -b "%NOVAORYN_GIT_BRANCH%" "%~dp0"
  if errorlevel 1 (
    echo [FAIL] Could not initialise the NovaOrynIDE Git repository.
    exit /b 1
  )
)

pushd "%~dp0" >nul

rem Always make the requested repository authoritative for this build tree.
git remote get-url origin >nul 2>&1
if errorlevel 1 (
  git remote add origin "%NOVAORYN_GIT_REMOTE%"
  if errorlevel 1 goto :git_fail
) else (
  git remote set-url origin "%NOVAORYN_GIT_REMOTE%"
  if errorlevel 1 goto :git_fail
)

git branch -M "%NOVAORYN_GIT_BRANCH%"
if errorlevel 1 goto :git_fail

rem If this is a freshly extracted FullSource tree and the remote already has history,
rem attach the new local repository to origin/main without overwriting the working tree.
git rev-parse --verify HEAD >nul 2>&1
if errorlevel 1 (
  git fetch origin "%NOVAORYN_GIT_BRANCH%" >nul 2>&1
  git show-ref --verify --quiet "refs/remotes/origin/%NOVAORYN_GIT_BRANCH%"
  if not errorlevel 1 (
    echo [INFO] Adopting existing origin/%NOVAORYN_GIT_BRANCH% history while preserving this source tree.
    git update-ref "refs/heads/%NOVAORYN_GIT_BRANCH%" "refs/remotes/origin/%NOVAORYN_GIT_BRANCH%"
    if errorlevel 1 goto :git_fail
    git reset --mixed HEAD >nul
    if errorlevel 1 goto :git_fail
  )
)

rem Use the developer's configured Git identity when available.  A local fallback
rem keeps unattended IDE builds able to create the initial commit.
git config user.name >nul 2>&1
if errorlevel 1 git config user.name "NovaOrynIDE Build"
git config user.email >nul 2>&1
if errorlevel 1 git config user.email "novaorynide-build@users.noreply.github.com"

echo [INFO] Staging source tree; .gitignore excludes tool downloads and build output.
git add -A
if errorlevel 1 goto :git_fail

git diff --cached --quiet
if errorlevel 1 (
  echo [INFO] Committing NovaOryn IDE 0.1.52 source changes.
  git commit -m "NovaOryn IDE 0.1.52"
  if errorlevel 1 goto :git_fail
) else (
  echo [INFO] No source changes require a new commit.
)

echo [INFO] Pushing %NOVAORYN_GIT_BRANCH% to %NOVAORYN_GIT_REMOTE%...
git push -u origin "%NOVAORYN_GIT_BRANCH%"
if errorlevel 1 goto :git_fail

echo [ OK ] NovaOryn IDE source committed and pushed to %NOVAORYN_GIT_REMOTE%.
popd >nul
exit /b 0

:git_fail
set "GIT_RESULT=!errorlevel!"
if "!GIT_RESULT!"=="0" set "GIT_RESULT=1"
echo [FAIL] NovaOryn IDE source commit/push failed with exit code !GIT_RESULT!.
echo [INFO] The IDE build itself completed successfully; resolve the Git/authentication error and run the build again.
popd >nul
exit /b !GIT_RESULT!
