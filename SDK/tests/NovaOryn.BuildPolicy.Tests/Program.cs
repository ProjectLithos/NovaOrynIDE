string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string projectRecognizer=File.ReadAllText(Path.Combine(root,"src","NovaOryn.VisualStudio","NovaOrynProjectRecognizer.cs"));
Require(projectRecognizer.Contains("TryGetProjectDirectory(Project project)",StringComparison.Ordinal),
        "Visual Studio project recognizer must resolve an EnvDTE Project to its project directory.");
Require(projectRecognizer.Contains("ThreadHelper.ThrowIfNotOnUIThread()",StringComparison.Ordinal),
        "Visual Studio project recognizer must assert the main thread before accessing EnvDTE.Project properties.");
string configurationServiceSource=File.ReadAllText(Path.Combine(root,"src","NovaOryn.VisualStudio","NovaOrynConfigurationService.cs"));
Require(configurationServiceSource.Contains("_dte.SelectedItems",StringComparison.Ordinal) &&
        !configurationServiceSource.Contains("_dte.ToolWindows",StringComparison.Ordinal),
        "Configuration Pages must resolve the selected project through DTE.SelectedItems rather than unavailable DTE.ToolWindows.");
string packageSource=File.ReadAllText(Path.Combine(root,"src","NovaOryn.VisualStudio","NovaOrynPackage.cs"));
Require(packageSource.Contains("FileAndForget(\"NovaOryn/ConfigurationPages\")",StringComparison.Ordinal),
        "The first-run Configuration Pages joinable task must be explicitly observed.");
string configWindow=File.ReadAllText(Path.Combine(root,"src","NovaOryn.VisualStudio","NovaOrynConfigurationWindow.cs"));
Require(configWindow.Contains("1. Architecture",StringComparison.Ordinal)&&configWindow.Contains("2. Kernel Model",StringComparison.Ordinal)&&configWindow.Contains("3. Work Areas",StringComparison.Ordinal)&&configWindow.Contains("4. Summary",StringComparison.Ordinal),"Visual Studio configuration UI must expose all four NovaOryn configuration pages.");
string configService=File.ReadAllText(Path.Combine(root,"src","NovaOryn.VisualStudio","NovaOrynConfigurationService.cs"));
Require(configService.Contains("NovaOryn.Configuration.props",StringComparison.Ordinal)&&configService.Contains("NovaOrynDefaultExecutionDomain",StringComparison.Ordinal),"Configuration apply must persist build-consumable MSBuild metadata.");
string fatFsSource=File.ReadAllText(Path.Combine(root,"src","NovaOryn.Filesystem.FatFs","FatFs.cs"));
Require(fatFsSource.Contains("KernelFileSystemType.Fat12",StringComparison.Ordinal)&&fatFsSource.Contains("KernelFileSystemType.Fat16",StringComparison.Ordinal)&&fatFsSource.Contains("KernelFileSystemType.Fat32",StringComparison.Ordinal),"Selectable FatFs module must register FAT12/FAT16/FAT32 providers.");
Require(fatFsSource.Contains("KernelStorage.ReadVolumeBlocks",StringComparison.Ordinal)&&fatFsSource.Contains("KernelStorage.WriteVolumeBlocks",StringComparison.Ordinal)&&fatFsSource.Contains("KernelStorage.Flush",StringComparison.Ordinal),"Selectable FatFs module must use only the generic NovaOryn block-device boundary.");
Require(!File.Exists(Path.Combine(root,"src","NovaOryn.Kernel.Storage","KernelFat32.cs")),"Generic Kernel.Storage must not contain a baked-in FAT provider.");
string storageProject=File.ReadAllText(Path.Combine(root,"src","NovaOryn.Kernel.Storage","NovaOryn.Kernel.Storage.csproj"));
Require(storageProject.Contains("<Compile Remove=\"KernelFat32.cs\"",StringComparison.Ordinal),
        "Kernel.Storage project must explicitly exclude a stale KernelFat32.cs left by an overlay update.");
string mainBuildScript=File.ReadAllText(Path.Combine(root,"Build-NovaOryn.ps1"));
Require(mainBuildScript.Contains("obsoleteBuiltInFileSystemFiles",StringComparison.Ordinal) &&
        mainBuildScript.Contains("Removing obsolete built-in filesystem source",StringComparison.Ordinal),
        "Build-NovaOryn must self-migrate stale pre-selectable-filesystem FAT source.");
string solution = Read(root, "NovaOryn.sln");
string build = Read(root, "Build-NovaOryn.ps1");
Require(solution.Contains("Release|Any CPU", StringComparison.Ordinal), "Solution must define Release|Any CPU.");
Require(solution.Contains("NovaOryn.Architecture.Arm64", StringComparison.Ordinal), "ARM64 architecture project must remain in the solution.");
Require(build.Contains("--property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Build must select Any CPU explicitly.");
Require(build.Contains("Kernel build mode: FAST", StringComparison.Ordinal), "Normal kernel builds must use the fast selected-project path.");
Require(build.Contains("if ($Validate)", StringComparison.Ordinal), "Exhaustive SDK validation must be opt-in rather than mandatory for every kernel build.");
Require(build.Contains("$requiredToolProjects", StringComparison.Ordinal), "Fast kernel builds must build only the required NovaOryn host tools before compiling the selected kernel dependency graph.");
string buildBatch = Read(root, "Build-NovaOryn.bat");
Require(!buildBatch.Contains("Build-NovaOrynDocumentation", StringComparison.Ordinal), "Normal Build-NovaOryn.bat must not regenerate documentation before every kernel build.");
string validateBatch = Read(root, "Validate-NovaOryn.bat");
string validateScript = Read(root, "Validate-NovaOryn.ps1");
Require(validateBatch.Contains("Validate-NovaOryn.ps1", StringComparison.Ordinal), "Validate-NovaOryn.bat must expose exhaustive validation explicitly.");
Require(validateScript.Contains("Build-NovaOrynDocumentation.ps1", StringComparison.Ordinal) && validateScript.Contains("-Validate", StringComparison.Ordinal), "Explicit validation must retain documentation generation and the exhaustive Build-NovaOryn validation path.");
Require(build.Contains("--ilc $ilc", StringComparison.Ordinal), "Build must pass the repository-pinned ILC executable.");
Require(build.Contains("NovaOryn.Time.Tests", StringComparison.Ordinal), "Build must execute independent timers and clocks tests.");
Require(build.Contains("NovaOryn.Smp.Tests", StringComparison.Ordinal), "Build must execute independent SMP and per-CPU state tests.");
Require(build.Contains("NovaOryn.Scheduler.Tests", StringComparison.Ordinal), "Build must execute independent scheduler and threads tests.");
Require(build.Contains("NovaOryn.Protection.Tests", StringComparison.Ordinal), "Build must execute independent user/kernel separation tests.");
Require(build.Contains("NovaOryn.SystemCalls.Tests", StringComparison.Ordinal), "Build must execute independent system-call methodology tests.");
Require(build.Contains("NovaOryn.Processes.Tests", StringComparison.Ordinal), "Build must execute independent process and executable-loading tests.");
Require(build.Contains("NovaOryn.Storage.Tests", StringComparison.Ordinal), "Build must execute independent storage and filesystem tests.");
Require(build.Contains("UserMode.asm", StringComparison.Ordinal), "Build must assemble the x64 ring-3 entry path.");
Require(build.Contains("native\\x64\\Syscalls.asm", StringComparison.Ordinal), "Build must assemble the x64 SYSCALL/SYSRET entry object.");
Require(build.Contains("build $systemCallTestProject --configuration $Configuration --property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Standalone system-call test build must use Any CPU so its output path matches the runner.");
Require(build.Contains("tests\\NovaOryn.SystemCalls.Tests\\bin\\Any CPU\\$Configuration\\net10.0\\NovaOryn.SystemCalls.Tests.dll", StringComparison.Ordinal), "System-call runner must execute the Any CPU output it just built.");
Require(build.Contains("build $policyTestProject --configuration $Configuration --property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Standalone policy-test builds must use Any CPU so their output path matches the runner.");
Require(build.Contains("build $timeTestProject --configuration $Configuration --property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Standalone timers-and-clocks test build must use Any CPU so its output path matches the runner.");
Require(build.Contains("build $smpTestProject --configuration $Configuration --property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Standalone SMP test build must use Any CPU so its output path matches the runner.");
Require(build.Contains("build $schedulerTestProject --configuration $Configuration --property:Platform=\"Any CPU\"", StringComparison.Ordinal), "Standalone scheduler test build must use Any CPU so its output path matches the runner.");
Require(build.Contains("tests\\$policyTestProgram\\bin\\Any CPU\\$Configuration\\net10.0\\$policyTestProgram.dll", StringComparison.Ordinal), "Standalone policy-test runner must execute the Any CPU output it just built.");
Require(build.Contains("tests\\NovaOryn.Time.Tests\\bin\\Any CPU\\$Configuration\\net10.0\\NovaOryn.Time.Tests.dll", StringComparison.Ordinal), "Timers-and-clocks runner must execute the Any CPU output it just built.");
Require(build.Contains("tests\\NovaOryn.Smp.Tests\\bin\\Any CPU\\$Configuration\\net10.0\\NovaOryn.Smp.Tests.dll", StringComparison.Ordinal), "SMP runner must execute the Any CPU output it just built.");
Require(build.Contains("tests\\NovaOryn.Scheduler.Tests\\bin\\Any CPU\\$Configuration\\net10.0\\NovaOryn.Scheduler.Tests.dll", StringComparison.Ordinal), "Scheduler runner must execute the Any CPU output it just built.");
string qemuLauncher = Read(root, "src/NovaOryn.QemuLauncher/Program.cs");
Require(qemuLauncher.Contains("return 90;", StringComparison.Ordinal), "QEMU runtime acceptance must allow the Debug/TCG boot path a 90-second default window.");
Require(qemuLauncher.Contains("NovaOryn KMain started, but interactive console readiness was not reached", StringComparison.Ordinal), "QEMU runtime failures must distinguish KMain progress from interactive console readiness.");
Require(qemuLauncher.Contains("QEMU serial tail follows", StringComparison.Ordinal), "QEMU runtime failures must surface the serial tail for diagnosis.");
Require(qemuLauncher.Contains("Environment.ProcessorCount", StringComparison.Ordinal), "QEMU launcher must detect the host logical processor count dynamically.");
Require(qemuLauncher.Contains("return (hostLogicalProcessorCount + 1) / 2;", StringComparison.Ordinal), "QEMU launcher must allocate 50 percent of host logical processors, rounded up.");
Require(qemuLauncher.Contains("\"-smp\", qemuProcessorCount.ToString", StringComparison.Ordinal), "QEMU -smp must use the calculated 50-percent processor count instead of a hard-coded value.");
Require(!qemuLauncher.Contains("\"-smp\", \"1\"", StringComparison.Ordinal), "QEMU launcher must not force a single virtual CPU.");
foreach (string name in new[]{"NovaOryn.ApiPolicy.Tests","NovaOryn.BuildPolicy.Tests","NovaOryn.BootPolicy.Tests","NovaOryn.MemoryPolicy.Tests","NovaOryn.TemplatePolicy.Tests","NovaOryn.DocumentationPolicy.Tests","NovaOryn.ReleasePolicy.Tests"})
    Require(build.Contains(name, StringComparison.Ordinal), $"Build must execute independent policy program {name}.");
string smp = Read(root, "src/NovaOryn.Kernel.Smp/KernelSmp.cs");
Require(smp.Contains("private const UInt32 StartupIpi = 0x00000600U;", StringComparison.Ordinal), "xAPIC SIPI delivery must use startup mode without carrying the INIT level-assert bit.");
string scheduler = Read(root, "src/NovaOryn.Kernel.Scheduler/KernelScheduler.cs");
Require(scheduler.Contains("Native.SwitchThreadContext", StringComparison.Ordinal), "Scheduler must invoke the native x64 context-switch primitive.");
string nativeCpu = Read(root, "native/x64/Cpu.asm");
Require(nativeCpu.Contains("NovaOrynX64InitializeThreadContext", StringComparison.Ordinal)&&nativeCpu.Contains("NovaOrynX64SwitchThreadContext", StringComparison.Ordinal), "x64 native layer must provide thread-context initialization and switching.");
Require(nativeCpu.Contains("NovaOrynX64WritePort16", StringComparison.Ordinal)&&nativeCpu.Contains("NovaOrynX64ReadPort16", StringComparison.Ordinal), "x64 native layer must provide 16-bit I/O access for ACPI GAS registers.");
Require(nativeCpu.Contains("NovaOrynX64Pause", StringComparison.Ordinal), "x64 native layer must provide a PAUSE primitive for the interactive console idle loop.");
string kernelConsole = Read(root, "src/NovaOryn.Kernel.Console/KernelConsole.cs");
Require(kernelConsole.Contains("ServiceInput()", StringComparison.Ordinal) && kernelConsole.Contains("RunInteractive()", StringComparison.Ordinal) && !kernelConsole.Contains("PollInput()", StringComparison.Ordinal), "Kernel console must expose dispatch-driven input servicing and must not reintroduce direct polling after boot completion.");
string ps2 = Read(root, "src/NovaOryn.Kernel.Ps2/KernelPs2.cs");
Require(ps2.Contains("InputContractVersion = 3U", StringComparison.Ordinal) && ps2.Contains("SetKeyboardEventHandler", StringComparison.Ordinal) && ps2.Contains("pressed==wasPressed", StringComparison.Ordinal) && ps2.Contains("IsKeyPressed", StringComparison.Ordinal), "PS/2 driver must expose input contract version 3, current key state, and suppress hardware typematic duplicate make codes.");
Require(ps2.Contains("case 0x48:return Ps2Key.Up", StringComparison.Ordinal) && ps2.Contains("case 0x50:return Ps2Key.Down", StringComparison.Ordinal), "PS/2 driver must decode extended Set-1 Up/Down scan codes into decoded key events.");
Require(ps2.Contains("case 0x02:return Ps2Key.D1", StringComparison.Ordinal) && ps2.Contains("case 0x03:return Ps2Key.D2", StringComparison.Ordinal) && ps2.Contains("case 0x04:return Ps2Key.D3", StringComparison.Ordinal), "PS/2 driver must decode number keys 1-3 into decoded key events.");
string bootstrapKernel = Read(root, "src/NovaOryn.Kernel.Bootstrap/Kernel.cs");
Require(!bootstrapKernel.Contains("InputContractVersion < 2U", StringComparison.Ordinal), "Bootstrap must not runtime-compare the compile-time PS/2 input contract constant; the handler API itself is the compile-time contract.");
Require(bootstrapKernel.Contains("ServiceKeyboardRepeat", StringComparison.Ordinal) &&
        bootstrapKernel.Contains("KeyboardRepeatInitialDelayNanoseconds=300000000UL", StringComparison.Ordinal) &&
        bootstrapKernel.Contains("KeyboardRepeatIntervalNanoseconds=40000000UL", StringComparison.Ordinal) &&
        bootstrapKernel.Contains("if (_ps2RepeatActive && _ps2RepeatKey==input.Key) _ps2RepeatActive=false;", StringComparison.Ordinal),
        "Bootstrap keyboard repeat must be software controlled and cancel the PS/2 repeat immediately on key-up.");
Require(bootstrapKernel.Contains("if (_usbRepeatActive && _usbRepeatUsage==input.Usage) _usbRepeatActive=false;", StringComparison.Ordinal),
        "Bootstrap keyboard repeat must cancel USB repeat immediately on key-up.");
string templateKernel = Read(root, "templates/NovaOrynKernel/Kernel/Kernel.cs");
foreach (string inputConsumer in new[] { bootstrapKernel, templateKernel })
{
    Require(inputConsumer.Contains("Ps2Key.Up", StringComparison.Ordinal) && inputConsumer.Contains("KernelConsole.ScrollUp", StringComparison.Ordinal) && inputConsumer.Contains("Ps2Key.Down", StringComparison.Ordinal) && inputConsumer.Contains("KernelConsole.ScrollDown", StringComparison.Ordinal), "Decoded keyboard input consumer must map Up/Down keys to console scroll actions.");
    Require(inputConsumer.Contains("Ps2Key.D1", StringComparison.Ordinal) && inputConsumer.Contains("SetFontPreset(1U)", StringComparison.Ordinal) && inputConsumer.Contains("Ps2Key.D2", StringComparison.Ordinal) && inputConsumer.Contains("SetFontPreset(2U)", StringComparison.Ordinal) && inputConsumer.Contains("Ps2Key.D3", StringComparison.Ordinal) && inputConsumer.Contains("SetFontPreset(3U)", StringComparison.Ordinal), "Decoded keyboard input consumer must map number keys 1-3 to font presets.");
Require(inputConsumer.Contains("input.Control", StringComparison.Ordinal) && inputConsumer.Contains("SetFramebufferBufferCount(1U)", StringComparison.Ordinal) && inputConsumer.Contains("SetFramebufferBufferCount(2U)", StringComparison.Ordinal) && inputConsumer.Contains("SetFramebufferBufferCount(3U)", StringComparison.Ordinal), "Decoded keyboard input consumer must map Ctrl+1/2/3 to single/double/triple framebuffer buffering.");
Require(inputConsumer.Contains("RegisterGet(33U", StringComparison.Ordinal) && inputConsumer.Contains("RegisterSet(33U", StringComparison.Ordinal) && inputConsumer.Contains("RegisterGet(34U", StringComparison.Ordinal) && inputConsumer.Contains("RegisterSet(34U", StringComparison.Ordinal), "Bootstrap must expose font and buffering presets through NovaOryn Get/Set services 33 and 34.");
}

Require(!kernelConsole.Contains("ReadPort8(0x60", StringComparison.Ordinal) && !kernelConsole.Contains("ReadPort8(0x64", StringComparison.Ordinal), "Kernel console must not read i8042 ports after KernelPs2 owns PS/2 input.");
string framebufferConsole = Read(root, "src/NovaOryn.Kernel.Console/FramebufferConsole.cs");
string framebufferBuffering = Read(root, "src/NovaOryn.Kernel.Console/FramebufferBuffering.cs");
Require(framebufferBuffering.Contains("Single = 1", StringComparison.Ordinal) && framebufferBuffering.Contains("Double = 2", StringComparison.Ordinal) && framebufferBuffering.Contains("Triple = 3", StringComparison.Ordinal), "Framebuffer console must expose single, double, and triple buffering modes.");
Require(framebufferConsole.Contains("ConfigureBuffers", StringComparison.Ordinal) && framebufferConsole.Contains("Present()", StringComparison.Ordinal) && framebufferConsole.Contains("CopyRegion", StringComparison.Ordinal) && framebufferConsole.Contains("MarkDirtyRectangle", StringComparison.Ordinal), "Framebuffer console must implement dirty-region software backbuffer presentation rather than copying a full frame for every character.");
Require(!framebufferConsole.Contains("CopyFrame(_drawBuffer, _address)", StringComparison.Ordinal), "Framebuffer Present must not copy the entire draw buffer to GOP for every character.");
Require(framebufferConsole.Contains("ScrollbackCapacity", StringComparison.Ordinal) && framebufferConsole.Contains("RedrawHistory", StringComparison.Ordinal), "Framebuffer console must retain and redraw scrollback history.");
string acpiPlatform = Read(root, "src/NovaOryn.Kernel.Acpi/KernelAcpiPlatform.cs");
Require(acpiPlatform.Contains("KernelAcpiFadt", StringComparison.Ordinal)&&acpiPlatform.Contains("KernelAcpiEc", StringComparison.Ordinal)&&acpiPlatform.Contains("KernelAcpiPower", StringComparison.Ordinal), "ACPI platform layer must provide FADT, EC and power services.");
Require(acpiPlatform.Contains("TryFindS5", StringComparison.Ordinal)&&!acpiPlatform.Contains("SLP_TYPa = 5", StringComparison.Ordinal), "ACPI shutdown must obtain S5 sleep types from AML rather than hard-coded emulator values.");
string nativeRuntime = Read(root, "native/x64/Runtime.asm");
Require(nativeRuntime.Contains("global __security_cookie", StringComparison.Ordinal), "Freestanding runtime must export the Win64 compiler security cookie required by ILC stack protection.");
Require(nativeRuntime.Contains("global __security_cookie_complement", StringComparison.Ordinal), "Freestanding runtime must export the compiler security-cookie complement.");
Require(nativeRuntime.Contains("rdtsc", StringComparison.Ordinal) && nativeRuntime.Contains("NovaOrynRuntimeInitialize", StringComparison.Ordinal), "Freestanding runtime must seed the compiler security cookie before managed execution.");
string nativeSyscalls = Read(root, "native/x64/Syscalls.asm");
Require(nativeSyscalls.Contains("global NovaOrynX64SyscallEntry", StringComparison.Ordinal) && nativeSyscalls.Contains("sysret", StringComparison.Ordinal), "x64 native layer must provide a real SYSCALL/SYSRET entry and return path.");
Require(nativeSyscalls.Contains("swapgs", StringComparison.Ordinal) && nativeSyscalls.Contains("IA32_LSTAR", StringComparison.Ordinal), "x64 syscall entry must configure LSTAR and use SWAPGS per-CPU state.");
Require(nativeSyscalls.Contains("NovaOrynManagedSyscallDispatch", StringComparison.Ordinal), "x64 syscall entry must dispatch through the managed syscall core.");
string systemCalls = Read(root, "src/NovaOryn.Kernel.SystemCalls/KernelSystemCalls.cs");
Require(systemCalls.Contains("RegisterGet", StringComparison.Ordinal) && systemCalls.Contains("RegisterSet", StringComparison.Ordinal) && systemCalls.Contains("RegisterEvent", StringComparison.Ordinal), "System-call core must support custom NovaOryn Get, Set, and Event handlers.");
Require(systemCalls.Contains("RegisterLinux", StringComparison.Ordinal) && systemCalls.Contains("RegisterNt", StringComparison.Ordinal), "System-call core must support Linux-style and NT-style custom service handlers.");
Require(systemCalls.Contains("TryCopyFromUser", StringComparison.Ordinal) && systemCalls.Contains("TryCopyToUser", StringComparison.Ordinal), "System-call core must provide guarded user-memory copy APIs before enabling SMAP.");
string compiler = Read(root, "src/NovaOryn.ManagedCompiler/Program.cs");
Require(!compiler.Contains("\"publish\"", StringComparison.Ordinal), "ManagedCompiler must not use dotnet publish for the freestanding bootstrap.");
foreach (string token in new[]{"--systemmodule","--targetos:win","--targetarch:x64","--nativelib","--directpinvoke:*","--noscan","--reflectiondata:none","--nopreinitstatics"})
    Require(compiler.Contains(token, StringComparison.Ordinal), $"Direct ILC invocation is missing {token}.");
string linker=Read(root,"src/NovaOryn.Linker/Program.cs");
Require(linker.Contains("GetProperty(\"nativeObject\")", StringComparison.Ordinal), "Linker must consume the direct ILC object.");
string protection = Read(root, "src/NovaOryn.Kernel.Protection/KernelProtection.cs");
Require(protection.Contains("EnableKernelWriteProtect", StringComparison.Ordinal) && protection.Contains("SupportsSmep", StringComparison.Ordinal), "Protection layer must enable supervisor write protection and detect SMEP.");
Require(protection.Contains("KernelVirtualMemoryProtection.User", StringComparison.Ordinal), "User mappings must explicitly set the user-accessible page-table bit.");
string protectionMath = Read(root, "src/NovaOryn.Kernel.Protection/KernelProtectionMath.cs");
Require(protectionMath.Contains("UInt64.MaxValue", StringComparison.Ordinal), ".NET-compatible protection code must use System.UInt64.MaxValue rather than a freestanding magic constant.");
string cpuProtection = Read(root, "native/x64/Cpu.asm");
Require(cpuProtection.Contains("CR0.WP", StringComparison.Ordinal) && cpuProtection.Contains("CR4.SMEP", StringComparison.Ordinal), "x64 native layer must implement hardware privilege protections.");
string coreLib = Read(root, "src/NovaOryn.Freestanding.CoreLib/CoreLib.cs");
foreach ((string primitive, string minValue, string maxValue) in new[]{
    ("SByte", "-128", "127"), ("Byte", "0", "255"),
    ("Int16", "-32768", "32767"), ("UInt16", "0", "65535"),
    ("Int32", "-2147483648", "2147483647"), ("UInt32", "0U", "0xFFFFFFFFU"),
    ("Int64", "-9223372036854775808L", "9223372036854775807L"),
    ("UInt64", "0UL", "0xFFFFFFFFFFFFFFFFUL")})
{
    Require(coreLib.Contains($"struct {primitive} {{ public const {primitive} MinValue = {minValue}; public const {primitive} MaxValue = {maxValue}; }}", StringComparison.Ordinal), $"Freestanding CoreLib must expose .NET-compatible MinValue/MaxValue constants for System.{primitive}.");
}
Require(coreLib.Contains("internal static unsafe class SpanHelpers", StringComparison.Ordinal), "Freestanding CoreLib must provide System.SpanHelpers for NativeAOT block operations.");
Require(coreLib.Contains("ClearWithoutReferences(ref Byte destination, UIntPtr byteCount)", StringComparison.Ordinal), "Freestanding CoreLib must provide SpanHelpers.ClearWithoutReferences.");
Require(coreLib.Contains("Memmove(ref Byte destination, ref Byte source, UIntPtr byteCount)", StringComparison.Ordinal), "Freestanding CoreLib must provide overlap-safe SpanHelpers.Memmove.");
Require(coreLib.Contains("public Int32 Pack;", StringComparison.Ordinal), "Freestanding CoreLib StructLayoutAttribute must expose the standard Pack field.");
Require(coreLib.Contains("public Int32 Size;", StringComparison.Ordinal), "Freestanding CoreLib StructLayoutAttribute must expose the standard Size field.");
Require(coreLib.Contains("public CharSet CharSet;", StringComparison.Ordinal), "Freestanding CoreLib StructLayoutAttribute must expose the standard CharSet field.");
Require(coreLib.Contains("public LayoutKind Value { get; }", StringComparison.Ordinal), "Freestanding CoreLib StructLayoutAttribute must expose the standard Value property.");

string interruptBroker = Read(root,"src/NovaOryn.Kernel.InterruptBroker/KernelInterruptBroker.cs");
string nvmeDriver = Read(root,"src/NovaOryn.Kernel.Nvme/KernelNvme.cs");
Require(interruptBroker.Contains("KernelDrivers.InstallInterruptBroker",StringComparison.Ordinal),"Opaque interrupt broker must install itself into the driver framework.");
Require(interruptBroker.Contains("TryProgramMsix",StringComparison.Ordinal)&&interruptBroker.Contains("TryProgramMsi",StringComparison.Ordinal)&&interruptBroker.Contains("TryRouteIntx",StringComparison.Ordinal),"Interrupt broker must own MSI-X, MSI, and I/O APIC fallback selection.");
Require(!nvmeDriver.Contains("TryEnableMsi",StringComparison.Ordinal)&&!nvmeDriver.Contains("TryEnableMsix",StringComparison.Ordinal),"NVMe driver must not program MSI or MSI-X directly.");
Finish();


string Read(string root, string relative) => File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
void Require(bool condition, string message) { if (!condition) failures.Add(message); }
void Finish()
{
    if (failures.Count != 0) { foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}"); Environment.Exit(1); }
    Console.WriteLine("[ OK ] " + Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "policy") + " passed.");
}
static string FindRepositoryRoot(string start)
{
    DirectoryInfo? current = new(start);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "NovaOryn.sln"))) return current.FullName;
        current = current.Parent;
    }
    throw new InvalidOperationException("NovaOryn repository root was not found.");
}

