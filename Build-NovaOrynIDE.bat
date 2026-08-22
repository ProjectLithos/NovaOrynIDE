@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

rem Remove legacy root-level CommonJS verifier files after the 0.11.2 CJS reorganisation.
rem Only files directly under the IDE root are removed; CJS\*.cjs is preserved.
if exist "%~dp0*.cjs" (
  echo [INFO] Removing legacy root-level .cjs files...
  del /q "%~dp0*.cjs" >nul 2>&1
  if exist "%~dp0*.cjs" (
    echo [FAIL] One or more legacy root-level .cjs files could not be removed.
    exit /b 1
  )
  echo [ OK ] Legacy root-level .cjs files removed.
)

rem Remove legacy root-level JSON files after the JSON source reorganisation.
rem JSON\ contains the authoritative root manifests; no *.json file is supported directly at the IDE root.
if exist "%~dp0*.json" (
  echo [INFO] Removing legacy root-level .json files...
  del /q "%~dp0*.json" >nul 2>&1
  if exist "%~dp0*.json" (
    echo [FAIL] One or more legacy root-level .json files could not be removed.
    exit /b 1
  )
  echo [ OK ] Legacy root-level .json files removed.
)

rem Remove legacy root-level text and script files after the 0.14.4 source reorganisation.
rem Build-NovaOrynIDE.bat and Run-NovaOrynIDE.bat are the only supported root scripts.
if exist "%~dp0*.txt" (
  echo [INFO] Removing legacy root-level .txt files...
  del /q "%~dp0*.txt" >nul 2>&1
  if exist "%~dp0*.txt" (
    echo [FAIL] One or more legacy root-level .txt files could not be removed.
    exit /b 1
  )
  echo [ OK ] Legacy root-level .txt files removed.
)
for %%E in (ps1 cmd sh js mjs py vbs wsf) do (
  if exist "%~dp0*.%%E" del /q "%~dp0*.%%E" >nul 2>&1
  if exist "%~dp0*.%%E" (
    echo [FAIL] One or more legacy root-level .%%E scripts could not be removed.
    exit /b 1
  )
)
for %%F in ("%~dp0*.bat") do (
  if /I not "%%~nxF"=="Build-NovaOrynIDE.bat" if /I not "%%~nxF"=="Run-NovaOrynIDE.bat" del /q "%%~fF" >nul 2>&1
)
for %%F in ("%~dp0*.bat") do (
  if /I not "%%~nxF"=="Build-NovaOrynIDE.bat" if /I not "%%~nxF"=="Run-NovaOrynIDE.bat" (
    echo [FAIL] Legacy root batch script could not be removed: %%~nxF
    exit /b 1
  )
)

rem NovaOryn IDE release contract: 0.16.1. VERSION line 1 is authoritative.
rem Resolve the multi-line VERSION manifest through PowerShell into a one-line scratch file.
rem CMD never reads VERSION directly, preventing the manifest body from becoming batch input.
set "NOVAORYN_VERSION_SCRATCH=%~dp0.toolchain\novaoryn-ide-version.txt"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Resolve-NovaOrynIDEVersion.ps1" -OutputPath "%NOVAORYN_VERSION_SCRATCH%"
if errorlevel 1 exit /b 1
set /p NOVAORYN_IDE_VERSION=<"%NOVAORYN_VERSION_SCRATCH%"
if not defined NOVAORYN_IDE_VERSION (
  echo [FAIL] Resolved NovaOryn IDE version is empty.
  exit /b 1
)
echo [INFO] NovaOryn IDE Build %NOVAORYN_IDE_VERSION%

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

set "BOOTSTRAP=%~dp0Scripts\Install-NovaOrynIDEToolchain.ps1"
if not exist "%BOOTSTRAP%" (
  echo [FAIL] Missing toolchain bootstrap script: %BOOTSTRAP%
  exit /b 1
)

echo [INFO] Verifying NovaOryn IDE %NOVAORYN_IDE_VERSION% build toolchain...
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
set "NOVAORYN_NPM_PREFIX=%~dp0.toolchain\NpmWorkspace"
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
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Validate-NovaOrynIDEDependencies.ps1"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE dependency compatibility verification failed.
  exit /b %RESULT%
)

if exist "%~dp0JSON\package-lock.json" (
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Manage-NovaOrynIDEPackageLock.ps1"
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
set "NOVAORYN_NODE_MODULES=%NOVAORYN_NPM_PREFIX%\node_modules"
set "NOVAORYN_BUILDSTATE_TOOL=%~dp0Scripts\Manage-NovaOrynIDEBuildState.ps1"
if not exist "%NOVAORYN_BUILDSTATE_TOOL%" (
  echo [FAIL] Missing build-state manager: %NOVAORYN_BUILDSTATE_TOOL%
  exit /b 1
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_BUILDSTATE_TOOL%" -Action Invalidate
if errorlevel 1 (
  echo [FAIL] Could not invalidate stale npm/Theia dependency state.
  exit /b 1
)

rem Stage the npm workspace under .toolchain so the IDE root itself contains no JSON manifests.
if not exist "%NOVAORYN_NPM_PREFIX%" mkdir "%NOVAORYN_NPM_PREFIX%"
copy /y "%~dp0JSON\package.json" "%NOVAORYN_NPM_PREFIX%\package.json" >nul
if errorlevel 1 (
  echo [FAIL] Could not stage JSON\package.json for npm.
  exit /b 1
)
if exist "%~dp0JSON\package-lock.json" copy /y "%~dp0JSON\package-lock.json" "%NOVAORYN_NPM_PREFIX%\package-lock.json" >nul
rmdir /s /q "%NOVAORYN_NPM_PREFIX%\applications" >nul 2>&1
rmdir /s /q "%NOVAORYN_NPM_PREFIX%\packages" >nul 2>&1
rmdir /s /q "%NOVAORYN_NPM_PREFIX%\CJS" >nul 2>&1
mklink /J "%NOVAORYN_NPM_PREFIX%\applications" "%~dp0applications" >nul 2>&1
mklink /J "%NOVAORYN_NPM_PREFIX%\packages" "%~dp0packages" >nul 2>&1
mklink /J "%NOVAORYN_NPM_PREFIX%\CJS" "%~dp0CJS" >nul 2>&1
if not exist "%NOVAORYN_NPM_PREFIX%\applications\electron\package.json" (
  echo [FAIL] Could not stage the npm workspace application junction.
  exit /b 1
)
rmdir /s /q "%~dp0node_modules" >nul 2>&1
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules" mkdir "%NOVAORYN_NPM_PREFIX%\node_modules"
mklink /J "%~dp0node_modules" "%NOVAORYN_NPM_PREFIX%\node_modules" >nul 2>&1
if not exist "%~dp0node_modules" (
  echo [FAIL] Could not create the root node_modules junction to the staged npm workspace.
  exit /b 1
)
set "NOVAORYN_RUNTIME_PACKAGE_CHECK=%~dp0Scripts\Validate-NovaOrynIDERuntimePackages.ps1"
if not exist "%NOVAORYN_RUNTIME_PACKAGE_CHECK%" (
  echo [FAIL] Missing runtime package verifier: %NOVAORYN_RUNTIME_PACKAGE_CHECK%
  exit /b 1
)

set "NOVAORYN_NEED_NPM_INSTALL=1"
if exist "%NOVAORYN_NPM_PREFIX%\node_modules" (
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_RUNTIME_PACKAGE_CHECK%"
  if not errorlevel 1 set "NOVAORYN_NEED_NPM_INSTALL=0"
)

if "!NOVAORYN_NEED_NPM_INSTALL!"=="1" (
  echo [INFO] Installed npm dependency tree is missing or incomplete; performing one clean dependency install.
  rmdir /s /q "%NOVAORYN_NPM_PREFIX%\node_modules" >nul 2>&1
  if exist "%NOVAORYN_BROWSER_MODULES%" rmdir /s /q "%NOVAORYN_BROWSER_MODULES%" >nul 2>&1
  mkdir "%NOVAORYN_NPM_PREFIX%\node_modules" >nul 2>&1
  echo [INFO] Installing NovaOryn IDE JavaScript dependencies, including development tools...
  pushd "%NOVAORYN_NPM_PREFIX%"
  call "%NOVAORYN_NPM%" install --include=dev --workspaces
  set "RESULT=!errorlevel!"
  popd
  if not "!RESULT!"=="0" (
    echo [FAIL] npm dependency installation failed with exit code !RESULT!.
    exit /b !RESULT!
  )
  if exist "%NOVAORYN_NPM_PREFIX%\package-lock.json" copy /y "%NOVAORYN_NPM_PREFIX%\package-lock.json" "%~dp0JSON\package-lock.json" >nul
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_RUNTIME_PACKAGE_CHECK%"
  if errorlevel 1 (
    echo [FAIL] npm completed but the required NovaOryn/Theia runtime package set is still incomplete.
    exit /b 1
  )
  echo [ OK ] Required NovaOryn/Theia runtime package set installed and verified.
) else (
  echo [ OK ] Reusing the verified npm dependency tree; all required runtime packages are present.
)

echo [INFO] Verifying Eclipse Theia CLI package...
if not exist "%NOVAORYN_NPM_PREFIX%\node_modules\@theia\cli\package.json" (
  echo [FAIL] @theia/cli package manifest is missing after dependency verification.
  exit /b 1
)
echo [ OK ] Eclipse Theia CLI package is installed.

echo [INFO] Verifying installed Theia/Electron runtime versions from the Electron workspace...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_BUILDSTATE_TOOL%" -Action VerifyDependencies
set "RESULT=!errorlevel!"
if not "!RESULT!"=="0" (
  echo [FAIL] Installed dependency tree does not match the NovaOryn dependency pins. Exit code !RESULT!.
  exit /b !RESULT!
)
echo [ OK ] Installed dependency verification completed and dependency state was recorded.

echo [INFO] Verifying Windows CA certificate module required by the Theia Node bundle...
echo [ OK ] Windows CA certificate module was verified by the authoritative dependency manifest check.

echo [INFO] Verifying root Theia CLI dependency surface...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDETheiaCliDependencies.cjs"
if errorlevel 1 (
  echo [FAIL] Root Theia CLI dependency verification failed.
  exit /b 1
)

echo [INFO] Running NovaOryn IDE production security gate...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Audit-NovaOrynIDE.ps1" -GateProduction
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE production security gate failed.
  exit /b %RESULT%
)

echo [INFO] Verifying TypeScript compiler for the NovaOryn Theia extension...
echo [ OK ] TypeScript compiler was verified by the authoritative dependency manifest check.

echo [INFO] Verifying React/JSX TypeScript declarations for the NovaOryn extension...
echo [ OK ] React/JSX TypeScript declarations were verified by the authoritative dependency manifest check.

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
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEAuthoritativeConfiguration.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn authoritative configuration verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying existing NovaOryn OS reconfiguration contract...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEReconfiguration.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn OS reconfiguration verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying system theme and syntax-highlighting contract...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEThemeAndSyntax.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE theme/syntax verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying build-owned GitHub source publishing contract...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEGitPublish.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE GitHub publishing verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying debugger breakpoint integration...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDebugBreakpoints.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE debugger breakpoint verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying exact NativeAOT source-debug integration...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEBundledSdk.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE bundled-SDK verification failed.
  exit /b %RESULT%
)

"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDESourceDebug.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE source-debug verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying conditional breakpoints and Watch expressions...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDebugExpressions.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE conditional-breakpoint/Watch verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying Memory viewer and named NativeAOT locals/arguments...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEMemoryLocals.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE Memory/locals verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying CPU/thread/process contexts, x64 call-stack unwinding, and NovaOryn application icon...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEExecutionUnwind.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE execution-context/x64-unwind/icon verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying page-table, heap and crash-dump debugging...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEPageHeapCrash.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE page-table/heap/crash-dump verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying professional OS Dashboard, Kernel Console, Hardware Tree and Test Explorer...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEProfessionalTools.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE professional OS tools verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying kernel tracing, boot analyser and performance profiler...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDETracingProfiler.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE tracing/profiler verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying bundled NovaOryn SDK API/ABI contract...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0SDK\Verify-NovaOrynSdkContract.ps1"
if errorlevel 1 ( echo [FAIL] Bundled NovaOryn SDK contract verification failed. & exit /b 1 )
echo [INFO] Verifying Driver Development Centre...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDriverCentre.cjs"
if errorlevel 1 (
  echo [FAIL] Driver Development Centre verification failed.
  exit /b 1
)
echo [INFO] Verifying Target Manager contract...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDETargetManager.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE Target Manager verification failed.
  exit /b %RESULT%
)
echo [INFO] Verifying OS-specific static analyzers...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEStaticAnalyzers.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE OS-specific static analyzer verification failed.
  exit /b %RESULT%
)
echo [INFO] Verifying Binary / Symbol Explorer...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEBinarySymbols.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE Binary / Symbol Explorer verification failed.
  exit /b %RESULT%
)
echo [INFO] Verifying Memory-map Visualiser...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEMemoryMap.cjs"
set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" (
  echo [FAIL] NovaOryn IDE Memory-map Visualiser verification failed.
  exit /b %RESULT%
)

echo [INFO] Verifying Syscall Explorer...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDESyscalls.cjs"
if errorlevel 1 (
  echo [FAIL] Syscall Explorer contract verification failed.
  exit /b 1
)

echo [INFO] Verifying Image / Disk Explorer...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEImageDiskExplorer.cjs"
if errorlevel 1 (
  echo [FAIL] Image / Disk Explorer contract verification failed.
  exit /b 1
)

echo [INFO] Verifying Physical-machine debugger transport...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEPhysicalDebugger.cjs"
if errorlevel 1 (
  echo [FAIL] Physical-machine debugger transport contract verification failed.
  exit /b 1
)

echo [INFO] Verifying formal kernel subsystem contracts...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDESubsystemContracts.cjs"
if errorlevel 1 (
  echo [FAIL] NovaOryn formal kernel subsystem contract verification failed.
  exit /b 1
)
echo [INFO] Verifying capability-based driver model...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDriverCapabilities.cjs"
if errorlevel 1 ( echo [FAIL] Capability-based driver model verification failed. & exit /b 1 )

echo [INFO] Verifying professional driver packaging...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDriverPackaging.cjs"
if errorlevel 1 ( echo [FAIL] Driver packaging verification failed. & exit /b 1 )

echo [INFO] Verifying unified NovaOryn device model...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEDeviceModel.cjs"
if errorlevel 1 ( echo [FAIL] Unified device model verification failed. & exit /b 1 )

echo [INFO] Verifying expanded NovaOryn freestanding CoreLib...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEFreestandingCoreLib.cjs"
if errorlevel 1 ( echo [FAIL] Freestanding CoreLib expansion verification failed. & exit /b 1 )

echo [INFO] Verifying structured NovaOryn kernel logging...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEKernelLogging.cjs"
if errorlevel 1 ( echo [FAIL] Structured kernel logging verification failed. & exit /b 1 )

echo [INFO] Verifying kernel runtime integration of subsystem contracts and driver capability grants...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEKernelRuntimeIntegration.cjs"
if errorlevel 1 ( echo [FAIL] Kernel runtime integration verification failed. & exit /b 1 )

echo [INFO] Verifying full generated-kernel bootstrap and legacy minimal-kernel migration...
"%NOVAORYN_NODE%" "%~dp0CJS\Verify-NovaOrynIDEFullKernelBootstrap.cjs"
if errorlevel 1 ( echo [FAIL] Full generated-kernel bootstrap verification failed. & exit /b 1 )

echo [INFO] Running final NovaOryn IDE verification suite...
pushd "%~dp0"
"%NOVAORYN_NODE%" "%~dp0CJS\Run-NovaOrynIDEFinalVerification.cjs"
set "RESULT=!errorlevel!"
popd
if not "!RESULT!"=="0" (
  echo [FAIL] Final NovaOryn IDE verification suite failed with exit code !RESULT!.
  exit /b !RESULT!
)
echo [ OK ] Final verification returned successfully; continuing to the Theia production build.

echo [INFO] Building NovaOryn IDE %NOVAORYN_IDE_VERSION%...
pushd "%NOVAORYN_NPM_PREFIX%"
call "%NOVAORYN_NPM%" run build
set "RESULT=!errorlevel!"
popd
if not "!RESULT!"=="0" (
  echo [FAIL] NovaOryn IDE build failed with exit code !RESULT!.
  exit /b !RESULT!
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

set "NOVAORYN_GENERATED_BUILD_VERSION=%~dp0applications\electron\lib\.novaoryn-build-version"
set "NOVAORYN_GENERATED_BUILD_STATE=%~dp0applications\electron\lib\.novaoryn-build-state.json"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_BUILDSTATE_TOOL%" -Action Stamp
if errorlevel 1 (
  echo [FAIL] Build succeeded but NovaOryn could not stamp the generated build-state markers.
  exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%NOVAORYN_BUILDSTATE_TOOL%" -Action Validate
if errorlevel 1 (
  echo [FAIL] Generated build-state markers do not agree with VERSION and the pinned runtime.
  exit /b 1
)

echo [ OK ] NovaOryn IDE %NOVAORYN_IDE_VERSION% build completed.

echo [INFO] Publishing NovaOryn IDE source to GitHub...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Publish-NovaOrynIDESource.ps1"
set "GIT_RESULT=!errorlevel!"
if not "!GIT_RESULT!"=="0" (
  echo [FAIL] NovaOryn IDE source publish failed with exit code !GIT_RESULT!.
  echo [INFO] The IDE build itself completed successfully; resolve the Git/authentication error and rerun publishing.
  exit /b !GIT_RESULT!
)
exit /b 0
