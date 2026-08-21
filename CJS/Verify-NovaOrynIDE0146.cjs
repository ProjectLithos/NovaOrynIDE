const fs=require('fs'),path=require('path');const root=path.resolve(__dirname,'..');let fail=0;function ok(c,m){console.log(`${c?'[ OK ]':'[FAIL]'} ${m}`);if(!c)fail++;}function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
const versionText=read('VERSION');const versionLines=versionText.split(/\r?\n/);ok(versionLines[0].trim()==='0.14.6','VERSION first line is 0.14.6');for(const required of ['Build-NovaOrynIDE.bat','Run-NovaOrynIDE.bat','JSON/package.json','applications/electron/package.json','packages/novaoryn-ide/package.json','packages/novaoryn-ide/src/node/novaoryn-project-service.ts','packages/novaoryn-ide/src/browser/novaoryn-widget.tsx','Scripts/Audit-NovaOrynIDE.bat','Scripts/Validate-NovaOrynIDEDependencies.ps1','Scripts/Manage-NovaOrynIDEBuildState.ps1','Scripts/Manage-NovaOrynIDEPackageLock.ps1','Scripts/Validate-NovaOrynIDERuntimePackages.ps1','JSON/Security-Baseline.json','CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs','CJS/Verify-NovaOrynIDEFullKernelBootstrap.cjs','CJS/Verify-NovaOrynIDEGitPublish.cjs','CJS/Verify-NovaOrynIDETargetManager.cjs'])ok(versionText.includes(required),`VERSION manifest includes ${required}`);
const versionBearing=['Build-NovaOrynIDE.bat','Run-NovaOrynIDE.bat','README.md','JSON/package.json','JSON/Security-Baseline.json','applications/electron/package.json','packages/novaoryn-ide/package.json','packages/novaoryn-ide/src/node/novaoryn-project-service.ts','packages/novaoryn-ide/src/browser/novaoryn-widget.tsx','packages/novaoryn-ide/lib/node/novaoryn-project-service.js','packages/novaoryn-ide/lib/browser/novaoryn-widget.js','CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs','CJS/Verify-NovaOrynIDEFullKernelBootstrap.cjs','CJS/Verify-NovaOrynIDEGitPublish.cjs','CJS/Verify-NovaOrynIDETargetManager.cjs','Scripts/Audit-NovaOrynIDE.bat','Scripts/Validate-NovaOrynIDEDependencies.ps1'];for(const p of versionBearing)ok(read(p).includes('0.14.6'),`${p} carries release 0.14.6`);for(const p of ['CJS/Verify-NovaOrynIDE0146.cjs','JSON/NovaOryn-IDE-0.14.6-ReleaseValidation.json','docs/Release-0.14.6.md','Ancillary/ChangedFiles.txt'])ok(versionText.includes(p),`VERSION lists current release file ${p}`);
const service=read('packages/novaoryn-ide/src/node/novaoryn-project-service.ts');ok(service.includes("const NOVAORYN_IDE_VERSION = '0.14.6'"),'backend reports 0.14.6');
ok(service.includes("network: 'none', graphics: 'virtio-gpu', usb: 'none'"),'hardware matrix starts from known-good NovaOryn QEMU control hardware');ok(service.includes("'-display', 'sdl'"),'hardware matrix control uses the same SDL display path as the proven QEMU launcher');ok(service.includes("testCase.network !== 'none'"),'matrix can keep networking absent for the control boot');ok(service.includes('Known-good control boot failed. Remaining matrix cases were skipped'),'matrix fails fast when the control boot fails');ok(service.includes('accepted.serialTail'),'matrix preserves serial diagnostics for failed boots');
const k=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs');for(const n of ['AllocationFailure','IoTimeout','DroppedInterrupt','DeviceReset','BadDma','CorruptPacket','PageFault','CpuOffline','FilesystemError'])ok(k.includes(n),`fault kind ${n}`);
const checks=[['SDK/src/NovaOryn.Kernel.Heap/KernelHeap.cs','AllocationFailure'],['SDK/src/NovaOryn.Kernel.Storage/KernelStorage.cs','IoTimeout'],['SDK/src/NovaOryn.Kernel.Drivers/KernelDrivers.cs','DroppedInterrupt'],['SDK/src/NovaOryn.Kernel.Drivers/KernelDrivers.cs','DeviceReset'],['SDK/src/NovaOryn.Kernel.Drivers/KernelDrivers.cs','BadDma'],['SDK/src/NovaOryn.Kernel.Networking/KernelNetworkQueue.cs','TryCorruptPacket'],['SDK/src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.cs','PageFault'],['SDK/src/NovaOryn.Kernel.Smp/KernelSmp.cs','CpuOffline'],['SDK/src/NovaOryn.Filesystem.FatFs/FatFs.cs','FilesystemError']];for(const [p,n] of checks)ok(read(p).includes(n),`${n} is wired into ${p}`);
ok(fs.existsSync(path.join(root,'SDK/tests/NovaOryn.FaultInjection.Tests/Program.cs')),'fault injection SDK test project exists');ok(read('SDK/src/NovaOryn.Kernel.SubsystemContracts/KernelTestRuntime.cs').includes('TryCorruptDmaAddress'),'DMA corruption helper exists');ok(read('SDK/src/NovaOryn.Kernel.SubsystemContracts/KernelTestRuntime.cs').includes('TryCorruptPacket'),'packet corruption helper exists');
const contribution=read('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts');
const engineeringHelper=/protected\s+async\s+showEngineeringWidget\s*\(widget:\s*any\)\s*:\s*Promise<void>\s*\{[\s\S]*?if\s*\(!widget\.isAttached\)\s*\{[\s\S]*?this\.shell\.addWidget\(widget,\s*\{\s*area:\s*['"]main['"]\s*\}\)[\s\S]*?\}/m;
ok(engineeringHelper.test(contribution),'Engineering tools default to the main document area when first opened');
ok(contribution.includes('showEngineeringWidget(this.consoleWidget)'),'Engineering Kernel Console uses the shared default-area opener');
ok(contribution.includes('showEngineeringWidget(this.testExplorerWidget)'),'Engineering Test Explorer uses the shared default-area opener');
ok(!/showEngineeringWidget\([^\n]+,\s*['"](?:left|right|bottom)['"]/.test(contribution),'Engineering commands do not force a permanent docking area');
const toolbar=read('packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx');
ok(/if\s*\(!this\.kernelConsole\.isAttached\)\s*\{[\s\S]*?this\.shell\.addWidget\(this\.kernelConsole,\s*\{\s*area:\s*['"]main['"]\s*\}\)/m.test(toolbar),'Run/debug Kernel Console defaults to a main-area document tab when first opened');
ok(!toolbar.includes('removeWidget(this.kernelConsole)'),'Run/debug does not forcibly redock an already attached Kernel Console');

const genericArch=read('SDK/src/NovaOryn.Kernel.Architecture/KernelArchitecture.cs');ok(genericArch.includes('namespace NovaOryn.Kernel.Architecture'),'generic kernel architecture API exists');
const x64Boundary=read('SDK/src/NovaOryn.Arch.X64/X64ArchitectureBoundary.cs');ok(x64Boundary.includes('namespace NovaOryn.Arch.X64'),'canonical NovaOryn.Arch.X64 implementation boundary exists');ok(x64Boundary.includes('NovaOryn.Kernel.Internal.X64'),'private x64 native ABI is consumed below the canonical boundary');
const platformProject=read('SDK/src/NovaOryn.Kernel.Platform.X64/NovaOryn.Kernel.Platform.X64.csproj');ok(platformProject.includes('NovaOryn.Arch.X64')&&!platformProject.includes('NovaOryn.Kernel.X64.LowLevel'),'Kernel.Platform.X64 depends on NovaOryn.Arch.X64, not the private low-level assembly');
for(const p of ['SDK/templates/NovaOrynKernel/NovaOrynKernel.csproj','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.csproj']){const t=read(p);ok(t.includes('NovaOryn.Kernel.Architecture'),`${p} exposes generic kernel architecture API`);ok(t.includes('NovaOryn.Arch.X64'),`${p} selects canonical x64 implementation`);ok(!t.includes('NovaOryn.Kernel.X64.LowLevel'),`${p} does not leak private x64 low-level assembly into generated kernel`);}
for(const p of ['SDK/templates/NovaOrynKernel/Boot/KernelPanicTransport.cs','SDK/src/NovaOryn.Kernel.Bootstrap/KernelPanicTransport.cs']){const t=read(p);ok(t.includes('NovaOryn.Arch.X64')&&!t.includes('NovaOryn.Kernel.Internal.X64'),`${p} uses x64 boundary instead of private native ABI`);}
const boundaryPolicy=JSON.parse(read('SDK/NovaOryn.ArchitectureBoundaries.json'));ok(boundaryPolicy.genericKernelApi==='NovaOryn.Kernel.Architecture','architecture policy defines generic API above implementations');ok(boundaryPolicy.architectureImplementations.includes('NovaOryn.Arch.X64'),'architecture policy defines canonical x64 implementation');ok(boundaryPolicy.reservedFutureImplementations.includes('NovaOryn.Arch.Arm64'),'architecture policy reserves canonical ARM64 implementation name');
const sdkSolution=read('SDK/NovaOryn.sln');ok(sdkSolution.includes('NovaOryn.Kernel.Architecture')&&sdkSolution.includes('NovaOryn.Arch.X64'),'new architecture boundary projects participate in the SDK solution');


const vsTemplate=read('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.vstemplate');ok(vsTemplate.includes('Sdk\\NovaOryn.Kernel.Architecture\\KernelArchitecture.cs')&&vsTemplate.includes('Sdk\\NovaOryn.Arch.X64\\X64ArchitectureBoundary.cs'),'Visual Studio template packages both architecture-boundary projects');


const build=read('Build-NovaOrynIDE.bat'); const run=read('Run-NovaOrynIDE.bat');
ok(build.includes('CJS\\Verify-NovaOrynIDE0146.cjs'),'Build invokes the current 0.14.6 release verifier');
ok(!build.includes('CJS\\Verify-NovaOrynIDE0145.cjs'),'Build does not invoke the previous 0.14.5 release verifier');
ok(build.includes('set /p NOVAORYN_IDE_VERSION=<"%~dp0VERSION"'),'Build reads authoritative VERSION line 1');
ok(run.includes('set /p NOVAORYN_IDE_VERSION=<"%~dp0VERSION"'),'Run reads authoritative VERSION line 1');
ok(build.includes('.novaoryn-build-state.json'),'Build writes structured generated-build state');
ok(run.includes('.novaoryn-build-state.json'),'Run validates structured generated-build state');
const buildStateTool=read('Scripts/Manage-NovaOrynIDEBuildState.ps1','Scripts/Manage-NovaOrynIDEPackageLock.ps1','Scripts/Validate-NovaOrynIDERuntimePackages.ps1');
ok(build.includes('Manage-NovaOrynIDEBuildState.ps1') && build.includes('-Action Invalidate'),'Build delegates stale-state invalidation to the build-state manager script');
ok(build.includes('-Action Stamp') && build.includes('-Action Validate'),'Build delegates marker creation and verification to the build-state manager script');
ok(run.includes('Manage-NovaOrynIDEBuildState.ps1') && run.includes('-Action Validate'),'Run delegates generated-build validation to the build-state manager script');
ok(!build.includes('powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$state='),'Build no longer embeds the fragile build-state PowerShell command line');
ok(!run.includes('powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$expected='),'Run no longer embeds the fragile build-state PowerShell command line');
ok(buildStateTool.includes("[ValidateSet('Invalidate','Stamp','Validate')]"),'Build-state manager exposes explicit Invalidate/Stamp/Validate actions');
ok(buildStateTool.includes('System.Text.UTF8Encoding($false)'),'Build-state manager stamps markers without BOM/trailing newline ambiguity');
ok(buildStateTool.includes("(Get-Content -LiteralPath $generatedVersion -Raw).Trim()"),'Build-state manager trims generated version marker before comparison');
ok(buildStateTool.includes('[string]$State.novaOrynIdeVersion -eq $Version'),'Build-state manager invalidates prior IDE-version state even when Theia/Electron are unchanged');


ok(!/powershell(?:\.exe)?[^\r\n]*\s-Command\s/i.test(build),'Build contains no inline PowerShell -Command expressions');
ok(!/powershell(?:\.exe)?[^\r\n]*\s-Command\s/i.test(run),'Run contains no inline PowerShell -Command expressions');
ok(build.includes('Manage-NovaOrynIDEPackageLock.ps1'),'Build delegates package-lock version handling to a self-contained PowerShell file');
ok(run.includes('Validate-NovaOrynIDERuntimePackages.ps1'),'Run delegates runtime package validation to a dedicated PowerShell file');
ok(fs.existsSync(path.join(root,'Scripts/Manage-NovaOrynIDEPackageLock.ps1')),'package-lock manager script exists');
ok(fs.existsSync(path.join(root,'Scripts/Validate-NovaOrynIDERuntimePackages.ps1')),'runtime package verifier script exists');
ok(build.includes('CJS\\Verify-NovaOrynIDE0146.cjs'),'Build invokes the current 0.14.6 release verifier');
ok(!build.includes('CJS\\Verify-NovaOrynIDE0145.cjs'),'Build does not invoke the previous 0.14.5 release verifier');


ok(!build.includes('-Root "%~dp0"'),'Build does not pass a trailing-backslash repository root through powershell.exe argv');
ok(!run.includes('-Root "%~dp0"'),'Run does not pass a trailing-backslash repository root through powershell.exe argv');
ok(!build.includes('-Version "%NOVAORYN_IDE_VERSION%"'),'Build-state PowerShell derives VERSION internally instead of receiving it through CMD argv');
ok(!run.includes('-Version "%NOVAORYN_IDE_VERSION%"'),'Run build-state validation derives VERSION internally instead of receiving it through CMD argv');
ok(buildStateTool.includes("Join-Path $PSScriptRoot '..'") && buildStateTool.includes("Get-Content -LiteralPath $versionPath -TotalCount 1"),'Build-state manager derives repository root and authoritative version internally');
const packageLockTool=read('Scripts/Manage-NovaOrynIDEPackageLock.ps1');
ok(packageLockTool.includes("Join-Path $PSScriptRoot '..'") && packageLockTool.includes("Join-Path $rootPath 'VERSION'"),'Package-lock manager derives repository root and VERSION internally');
const runtimeTool=read('Scripts/Validate-NovaOrynIDERuntimePackages.ps1');
ok(runtimeTool.includes("Join-Path $PSScriptRoot '..'") && runtimeTool.includes(".toolchain\\NpmWorkspace"),'Runtime package verifier derives the staged npm workspace internally');
ok(run.includes('call "%NOVAORYN_NPM%" --prefix "%NOVAORYN_NPM_PREFIX%" run start --workspace @novaoryn/ide-electron'),'Run starts npm explicitly against .toolchain\\NpmWorkspace rather than relying on CWD');
ok(!/powershell(?:\.exe)?[^\r\n]*-File[^\r\n]*-Root\s+"%~dp0"/i.test(build+run),'No top-level PowerShell -File invocation passes %~dp0 as a quoted trailing-backslash argument');


const installVerifyMarker='[ OK ] Installed dependency verification completed without a fallback reinstall.';
ok(build.includes('set "RESULT=!errorlevel!"') && build.includes(installVerifyMarker),'Build captures installed-dependency verifier exit code explicitly');
ok(!build.includes('Performing one clean dependency reinstall from the checked package manifests'),'Build has no redundant second npm reinstall fallback');
ok(build.includes('refusing a redundant reinstall loop'),'Build fails deterministically if the freshly installed dependency tree is invalid');
ok(build.indexOf(installVerifyMarker) < build.indexOf('Verifying Windows CA certificate module'),'Successful dependency verification continues into the remaining build pipeline');
ok(build.includes('-Action Stamp') && build.includes('-Action Validate'),'Build stamps and validates generated build state after successful compilation');

process.exit(fail?1:0);
