string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string vs=Path.Combine(root,"src","NovaOryn.VisualStudio","ProjectTemplates","CSharp","1033","NovaOrynKernel");
foreach((string rel,string auth) in new[]{
("Sdk/NovaOryn.Kernel.Memory/KernelPhysicalMemory.cs","src/NovaOryn.Kernel.Memory/KernelPhysicalMemory.cs"),
("Sdk/NovaOryn.Kernel.Memory/KernelPhysicalMemory.Bootstrap.cs","src/NovaOryn.Kernel.Memory/KernelPhysicalMemory.Bootstrap.cs"),
("Sdk/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.cs","src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.cs"),
("Sdk/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.DirectMap.cs","src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.DirectMap.cs"),
("Sdk/NovaOryn.Kernel.Console/KernelConsole.cs","src/NovaOryn.Kernel.Console/KernelConsole.cs"),
("Sdk/NovaOryn.Kernel.Console/FramebufferConsole.cs","src/NovaOryn.Kernel.Console/FramebufferConsole.cs"),
("Sdk/NovaOryn.Kernel.Console/FramebufferBuffering.cs","src/NovaOryn.Kernel.Console/FramebufferBuffering.cs"),
("Sdk/NovaOryn.Kernel.Console/BootContext.cs","src/NovaOryn.Kernel.Console/BootContext.cs"),
("Sdk/NovaOryn.Kernel.Acpi/KernelAcpi.cs","src/NovaOryn.Kernel.Acpi/KernelAcpi.cs"),
("Sdk/NovaOryn.Kernel.Acpi/KernelAcpiPlatform.cs","src/NovaOryn.Kernel.Acpi/KernelAcpiPlatform.cs"),
("Sdk/NovaOryn.Kernel.Time/KernelTime.cs","src/NovaOryn.Kernel.Time/KernelTime.cs"),
("Sdk/NovaOryn.Kernel.Time/KernelTimeContracts.cs","src/NovaOryn.Kernel.Time/KernelTimeContracts.cs"),
("Sdk/NovaOryn.Kernel.Time/KernelTimeMath.cs","src/NovaOryn.Kernel.Time/KernelTimeMath.cs"),
("Sdk/NovaOryn.Kernel.Smp/KernelSmp.cs","src/NovaOryn.Kernel.Smp/KernelSmp.cs"),
("Sdk/NovaOryn.Kernel.Smp/KernelSmpContracts.cs","src/NovaOryn.Kernel.Smp/KernelSmpContracts.cs"),
("Sdk/NovaOryn.Kernel.Smp/KernelSmpMath.cs","src/NovaOryn.Kernel.Smp/KernelSmpMath.cs"),
("Sdk/NovaOryn.Kernel.Scheduler/KernelScheduler.cs","src/NovaOryn.Kernel.Scheduler/KernelScheduler.cs"),
("Sdk/NovaOryn.Kernel.Scheduler/KernelSchedulerContracts.cs","src/NovaOryn.Kernel.Scheduler/KernelSchedulerContracts.cs"),
("Sdk/NovaOryn.Kernel.Scheduler/KernelSchedulerMath.cs","src/NovaOryn.Kernel.Scheduler/KernelSchedulerMath.cs"),
("Sdk/NovaOryn.Kernel.Processes/KernelProcesses.cs","src/NovaOryn.Kernel.Processes/KernelProcesses.cs"),
("Sdk/NovaOryn.Kernel.Storage/KernelStorage.cs","src/NovaOryn.Kernel.Storage/KernelStorage.cs"),
("Sdk/NovaOryn.Kernel.Storage/KernelStorageContracts.cs","src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs"),
("Sdk/NovaOryn.Kernel.Storage/KernelStorageMath.cs","src/NovaOryn.Kernel.Storage/KernelStorageMath.cs"),
("Sdk/NovaOryn.Kernel.Storage/KernelStorageQueue.cs","src/NovaOryn.Kernel.Storage/KernelStorageQueue.cs"),
("Sdk/NovaOryn.Kernel.Storage/KernelVfs.cs","src/NovaOryn.Kernel.Storage/KernelVfs.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelNetworking.cs","src/NovaOryn.Kernel.Networking/KernelNetworking.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelNetworkContracts.cs","src/NovaOryn.Kernel.Networking/KernelNetworkContracts.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelNetworkMath.cs","src/NovaOryn.Kernel.Networking/KernelNetworkMath.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelNetworkStack.cs","src/NovaOryn.Kernel.Networking/KernelNetworkStack.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelSockets.cs","src/NovaOryn.Kernel.Networking/KernelSockets.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelNetworkQueue.cs","src/NovaOryn.Kernel.Networking/KernelNetworkQueue.cs"),
("Sdk/NovaOryn.Kernel.Networking/KernelDhcpDns.cs","src/NovaOryn.Kernel.Networking/KernelDhcpDns.cs"),
("Sdk/NovaOryn.Kernel.Pci/KernelPci.cs","src/NovaOryn.Kernel.Pci/KernelPci.cs"),
("Sdk/NovaOryn.Kernel.Pci/PciContracts.cs","src/NovaOryn.Kernel.Pci/PciContracts.cs"),
("Sdk/NovaOryn.Kernel.Pci/PciMath.cs","src/NovaOryn.Kernel.Pci/PciMath.cs"),
("Sdk/NovaOryn.Kernel.Virtio/KernelVirtio.cs","src/NovaOryn.Kernel.Virtio/KernelVirtio.cs"),
("Sdk/NovaOryn.Kernel.Serial/KernelSerial.cs","src/NovaOryn.Kernel.Serial/KernelSerial.cs"),
("Sdk/NovaOryn.Kernel.Serial/SerialContracts.cs","src/NovaOryn.Kernel.Serial/SerialContracts.cs"),
("Sdk/NovaOryn.Kernel.Serial/SerialMath.cs","src/NovaOryn.Kernel.Serial/SerialMath.cs"),
("Sdk/NovaOryn.Kernel.Ps2/KernelPs2.cs","src/NovaOryn.Kernel.Ps2/KernelPs2.cs"),
("Sdk/NovaOryn.Kernel.Ps2/Ps2Contracts.cs","src/NovaOryn.Kernel.Ps2/Ps2Contracts.cs"),
("Sdk/NovaOryn.Kernel.Ps2/KeyboardLayouts.cs","src/NovaOryn.Kernel.Ps2/KeyboardLayouts.cs"),
("Sdk/NovaOryn.Kernel.Nvme/KernelNvme.cs","src/NovaOryn.Kernel.Nvme/KernelNvme.cs"),
("Sdk/NovaOryn.Kernel.Nvme/NvmeContracts.cs","src/NovaOryn.Kernel.Nvme/NvmeContracts.cs"),
("Sdk/NovaOryn.Kernel.Nvme/NvmeMath.cs","src/NovaOryn.Kernel.Nvme/NvmeMath.cs"),
("Sdk/NovaOryn.Kernel.Ahci/KernelAhci.cs","src/NovaOryn.Kernel.Ahci/KernelAhci.cs"),
("Sdk/NovaOryn.Kernel.Ahci/AhciContracts.cs","src/NovaOryn.Kernel.Ahci/AhciContracts.cs"),
("Sdk/NovaOryn.Kernel.Ahci/AhciMath.cs","src/NovaOryn.Kernel.Ahci/AhciMath.cs"),
("Sdk/NovaOryn.Kernel.Polling/KernelPolling.cs","src/NovaOryn.Kernel.Polling/KernelPolling.cs"),
("Sdk/NovaOryn.Kernel.InterruptDispatch/KernelInterruptDispatch.cs","src/NovaOryn.Kernel.InterruptDispatch/KernelInterruptDispatch.cs"),
("Sdk/NovaOryn.Kernel.InterruptBroker/KernelInterruptBroker.cs","src/NovaOryn.Kernel.InterruptBroker/KernelInterruptBroker.cs"),
("Sdk/NovaOryn.Kernel.InterruptBroker/KernelInterruptBrokerContracts.cs","src/NovaOryn.Kernel.InterruptBroker/KernelInterruptBrokerContracts.cs"),
("Sdk/NovaOryn.Kernel.InterruptBroker/KernelInterruptBrokerMath.cs","src/NovaOryn.Kernel.InterruptBroker/KernelInterruptBrokerMath.cs"),
("Sdk/NovaOryn.Kernel.TimerDispatch/KernelTimerDispatch.cs","src/NovaOryn.Kernel.TimerDispatch/KernelTimerDispatch.cs"),
("Sdk/NovaOryn.Kernel.E1000/KernelE1000.cs","src/NovaOryn.Kernel.E1000/KernelE1000.cs"),
("Sdk/NovaOryn.Kernel.E1000/E1000Contracts.cs","src/NovaOryn.Kernel.E1000/E1000Contracts.cs"),
("Sdk/NovaOryn.Kernel.E1000/E1000Math.cs","src/NovaOryn.Kernel.E1000/E1000Math.cs"),
("Sdk/NovaOryn.Kernel.Rtl8168/KernelRtl8168.cs","src/NovaOryn.Kernel.Rtl8168/KernelRtl8168.cs"),
("Sdk/NovaOryn.Kernel.Rtl8168/Rtl8168Contracts.cs","src/NovaOryn.Kernel.Rtl8168/Rtl8168Contracts.cs"),
("Sdk/NovaOryn.Kernel.Rtl8168/Rtl8168Math.cs","src/NovaOryn.Kernel.Rtl8168/Rtl8168Math.cs"),
("Sdk/NovaOryn.Kernel.Virtio/VirtioContracts.cs","src/NovaOryn.Kernel.Virtio/VirtioContracts.cs"),
("Sdk/NovaOryn.Kernel.Virtio/VirtioMath.cs","src/NovaOryn.Kernel.Virtio/VirtioMath.cs"),
("Sdk/NovaOryn.Kernel.X64.LowLevel/Native.cs","src/NovaOryn.Kernel.X64.LowLevel/Native.cs"),
("Sdk/NovaOryn.String/StringFormatter.cs","src/NovaOryn.String/StringFormatter.cs"),
("Sdk/NovaOryn.Freestanding.CoreLib/CoreLib.cs","src/NovaOryn.Freestanding.CoreLib/CoreLib.cs")})
{
 string a=Read(root,auth); string c=File.ReadAllText(Path.Combine(root,"templates","NovaOrynKernel",rel.Replace('/',Path.DirectorySeparatorChar))); string v=File.ReadAllText(Path.Combine(vs,rel.Replace('/',Path.DirectorySeparatorChar)));
 Require(a==c&&a==v,$"Template copy differs from authoritative source: {rel}");
}
string cliKernel=Read(root,"templates/NovaOrynKernel/Kernel/Kernel.cs");
string vsKernel=File.ReadAllText(Path.Combine(vs,"Kernel","Kernel.cs"));
        if (string.IsNullOrWhiteSpace(vsKernel))
            Fail("Visual Studio user Kernel.cs must not be empty.");
        string vsTemplate = File.ReadAllText(Path.Combine(vs,"NovaOrynKernel.vstemplate"));
        if (vsTemplate.Contains("OpenInEditor=\"true\" TargetFileName=\"Kernel\\Kernel.cs\"", StringComparison.Ordinal))
            Fail("Visual Studio kernel template must not auto-open Kernel.cs during project creation.");

string cliConsole=Read(root,"templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Console/KernelConsole.cs");
string cliFont=Read(root,"templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Console/BitmapFont.cs");
string vsConsole=File.ReadAllText(Path.Combine(vs,"Sdk","NovaOryn.Kernel.Console","KernelConsole.cs"));
string vsFont=File.ReadAllText(Path.Combine(vs,"Sdk","NovaOryn.Kernel.Console","BitmapFont.cs"));
Require(cliKernel.Contains("KernelConsole.Initialize(boot)",StringComparison.Ordinal)&&cliConsole.Contains("DefaultFontSize = BitmapFont.DefaultFontSize",StringComparison.Ordinal)&&cliFont.Contains("DefaultFontSize = 24U",StringComparison.Ordinal),"Command-line template must use the 24-pixel framebuffer font default.");
Require(vsKernel.Contains("KernelConsole.Initialize(boot)",StringComparison.Ordinal)&&vsConsole.Contains("DefaultFontSize = BitmapFont.DefaultFontSize",StringComparison.Ordinal)&&vsFont.Contains("DefaultFontSize = 24U",StringComparison.Ordinal),"Visual Studio template must use the 24-pixel framebuffer font default.");
Require(cliConsole.Contains("WriteUInt64(UInt64 value)",StringComparison.Ordinal)&&cliConsole.Contains("WriteByteSize(UInt64 bytes)",StringComparison.Ordinal)&&cliConsole.Contains("WriteFrequency(UInt64 hertz)",StringComparison.Ordinal)&&cliConsole.Contains("WriteDurationNanoseconds(UInt64 nanoseconds)",StringComparison.Ordinal),"Command-line template must provide human-readable numeric console formatting.");
Require(vsConsole.Contains("WriteUInt64(UInt64 value)",StringComparison.Ordinal)&&vsConsole.Contains("WriteByteSize(UInt64 bytes)",StringComparison.Ordinal)&&vsConsole.Contains("WriteFrequency(UInt64 hertz)",StringComparison.Ordinal)&&vsConsole.Contains("WriteDurationNanoseconds(UInt64 nanoseconds)",StringComparison.Ordinal),"Visual Studio template must provide human-readable numeric console formatting.");
Require(cliKernel.Contains("WriteUInt64(KernelAcpi.GetProcessorCount())",StringComparison.Ordinal)&&cliKernel.Contains("WriteFrequency(KernelTime.GetClockFrequencyHz())",StringComparison.Ordinal)&&cliKernel.Contains("WriteByteSize(physicalMemory.ManagedPages * 4096UL)",StringComparison.Ordinal)&&cliKernel.Contains("WriteDurationNanoseconds(KernelScheduler.GetQuantumNanoseconds())",StringComparison.Ordinal),"Command-line startup diagnostics must use human-readable number formats.");
Require(vsKernel.Contains("WriteUInt64(KernelAcpi.GetProcessorCount())",StringComparison.Ordinal)&&vsKernel.Contains("WriteFrequency(KernelTime.GetClockFrequencyHz())",StringComparison.Ordinal)&&vsKernel.Contains("WriteByteSize(physicalMemory.ManagedPages * 4096UL)",StringComparison.Ordinal)&&vsKernel.Contains("WriteDurationNanoseconds(KernelScheduler.GetQuantumNanoseconds())",StringComparison.Ordinal),"Visual Studio startup diagnostics must use human-readable number formats.");
string authoritativeFramebuffer = Read(root,"src/NovaOryn.Kernel.Console/FramebufferConsole.cs");
Require(authoritativeFramebuffer.Contains("AppendHistory(value)",StringComparison.Ordinal)&&authoritativeFramebuffer.Contains("ScrollUp()",StringComparison.Ordinal)&&authoritativeFramebuffer.Contains("ScrollDown()",StringComparison.Ordinal)&&authoritativeFramebuffer.Contains("RedrawHistory()",StringComparison.Ordinal),"Freestanding framebuffer console must retain history and support interactive scrollback redraw.");
Require(!cliKernel.Contains("DllImport",StringComparison.Ordinal)&&!vsKernel.Contains("DllImport",StringComparison.Ordinal),"Generated user kernel must not expose native imports.");
Require(!cliConsole.Contains("PollInput",StringComparison.Ordinal)&&!vsConsole.Contains("PollInput",StringComparison.Ordinal),"Generated console must not contain keyboard polling APIs.");
Require(cliKernel.Contains("KernelInterruptDispatch.Initialize()",StringComparison.Ordinal)&&cliKernel.Contains("KernelInterruptBroker.Initialize()",StringComparison.Ordinal)&&cliKernel.Contains("KernelTimerDispatch.Initialize()",StringComparison.Ordinal)&&vsKernel.Contains("KernelInterruptDispatch.Initialize()",StringComparison.Ordinal)&&vsKernel.Contains("KernelInterruptBroker.Initialize()",StringComparison.Ordinal)&&vsKernel.Contains("KernelTimerDispatch.Initialize()",StringComparison.Ordinal),"Generated kernels must initialize interrupt dispatch, the opaque interrupt broker, and timer dispatch.");
Require(!cliKernel.Contains("KernelPolling.Initialize()",StringComparison.Ordinal)&&!vsKernel.Contains("KernelPolling.Initialize()",StringComparison.Ordinal),"Generated default kernels must not enable the optional polling methodology.");
string authoritativeConsole = Read(root,"src/NovaOryn.Kernel.Console/KernelConsole.cs");
string authoritativeVirtio = Read(root,"src/NovaOryn.Kernel.Virtio/KernelVirtio.cs");
string authoritativeE1000 = Read(root,"src/NovaOryn.Kernel.E1000/KernelE1000.cs");
string authoritativeRtl = Read(root,"src/NovaOryn.Kernel.Rtl8168/KernelRtl8168.cs");
Require(!authoritativeConsole.Contains("PollInput",StringComparison.Ordinal)&&!authoritativeVirtio.Contains("PollAll",StringComparison.Ordinal)&&!authoritativeE1000.Contains("PollAll",StringComparison.Ordinal)&&!authoritativeRtl.Contains("PollAll",StringComparison.Ordinal),"Default console and network drivers must expose timer/interrupt service APIs rather than polling loops.");

string cliProject=Read(root,"templates/NovaOrynKernel/NovaOrynKernel.csproj");
string vsProject=File.ReadAllText(Path.Combine(vs,"NovaOrynKernel.csproj"));
        // Every explicit SDK ProjectReference in the kernel template must resolve
        // against the source tree before VSIX packaging neutralizes nested .csproj
        // filenames for Visual Studio template instantiation.
        var vsProjectXml = System.Xml.Linq.XDocument.Load(Path.Combine(vs,"NovaOrynKernel.csproj"));
        foreach (var reference in vsProjectXml.Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
        {
            string include = (string?)reference.Attribute("Include") ?? string.Empty;
            if (include.Contains("$(") || include.Contains("*")) continue;
            string referencedPath = Path.GetFullPath(Path.Combine(vs, include.Replace('\\', Path.DirectorySeparatorChar)));
            if (!File.Exists(referencedPath))
                Fail("Visual Studio kernel template nested SDK project references must resolve in source: " + include);
        }

foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Heap;",StringComparison.Ordinal),"Generated kernel using heap APIs must import NovaOryn.Kernel.Heap.");
 Require(kernel.Contains("KernelEarlyAllocator",StringComparison.Ordinal)&&kernel.Contains("KernelHeap",StringComparison.Ordinal)&&kernel.Contains("KernelHeapAllocation",StringComparison.Ordinal),"Generated kernel must demonstrate early allocator and heap APIs together.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Heap\\NovaOryn.Kernel.Heap.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Heap directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Heap","NovaOryn.Kernel.Heap.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Heap.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Heap","KernelHeap.cs")),"Generated SDK tree must contain KernelHeap.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Heap","KernelEarlyAllocator.cs")),"Generated SDK tree must contain KernelEarlyAllocator.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
 Require(kernel.Contains("using NovaOryn.Kernel.AddressSpace;",StringComparison.Ordinal)&&kernel.Contains("KernelAddressSpace",StringComparison.Ordinal),"Generated kernel using address-space APIs must import and use NovaOryn.Kernel.AddressSpace.");
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.AddressSpace\\NovaOryn.Kernel.AddressSpace.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.AddressSpace directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.AddressSpace","NovaOryn.Kernel.AddressSpace.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.AddressSpace.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.AddressSpace","KernelAddressSpace.cs")),"Generated SDK tree must contain KernelAddressSpace.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Acpi;",StringComparison.Ordinal)&&kernel.Contains("KernelAcpi.Initialize(boot)",StringComparison.Ordinal),"Generated kernel must initialize the high-level ACPI discovery service.");
 Require(kernel.Contains("KernelAcpiFadt.Initialize()",StringComparison.Ordinal)&&kernel.Contains("KernelAcpiPower.Initialize()",StringComparison.Ordinal)&&kernel.Contains("KernelAcpiEc.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize FADT, power and embedded-controller platform services.");
 Require(kernel.Contains("ACPI MADT, MCFG, HPET, FADT and platform power services online.",StringComparison.Ordinal),"Generated kernel must visibly report ACPI platform-driver initialization.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Acpi\\NovaOryn.Kernel.Acpi.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Acpi directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Acpi","NovaOryn.Kernel.Acpi.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Acpi.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Acpi","KernelAcpi.cs")),"Generated SDK tree must contain KernelAcpi.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Acpi","KernelAcpiPlatform.cs")),"Generated SDK tree must contain KernelAcpiPlatform.cs.");
}

foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Time;",StringComparison.Ordinal)&&kernel.Contains("KernelTime.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the high-level timers and clocks service.");
 Require(kernel.Contains("HPET, Local APIC timer, TSC, RTC/CMOS and invariant-TSC clock source online.",StringComparison.Ordinal),"Generated kernel must visibly report every required timer and clock facility.");
 Require(kernel.Contains("KernelRtcCmos.TryRead",StringComparison.Ordinal)&&kernel.Contains("KernelHpet.GetFrequencyHz",StringComparison.Ordinal)&&kernel.Contains("KernelTsc.GetFrequencyHz",StringComparison.Ordinal)&&kernel.Contains("KernelLocalApicTimer.IsAvailable",StringComparison.Ordinal),"Generated kernel must use high-level HPET, TSC, RTC/CMOS and Local APIC timer APIs.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Time\\NovaOryn.Kernel.Time.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Time directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","NovaOryn.Kernel.Time.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Time.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelTime.cs")),"Generated SDK tree must contain KernelTime.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelTimeContracts.cs")),"Generated SDK tree must contain KernelTimeContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelTimeMath.cs")),"Generated SDK tree must contain KernelTimeMath.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelRtcMath.cs")),"Generated SDK tree must contain KernelRtcMath.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelRtcCmos.cs")),"Generated SDK tree must contain KernelRtcCmos.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Time","KernelTimerFacilities.cs")),"Generated SDK tree must contain KernelTimerFacilities.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Serial;",StringComparison.Ordinal)&&kernel.Contains("KernelSerial.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the high-level serial facility after PCI and VirtIO discovery.");
 Require(kernel.Contains("using NovaOryn.Kernel.Ps2;",StringComparison.Ordinal)&&kernel.Contains("KernelPs2.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the i8042 PS/2 facility.");
 Require(kernel.Contains("English_UK",StringComparison.Ordinal)&&kernel.Contains("English_USA",StringComparison.Ordinal),"Generated kernel must report both installed keyboard layouts.");
 Require(kernel.Contains("Serial 16550 / PCI UART / VirtIO console:",StringComparison.Ordinal),"Generated kernel must visibly report 16550, PCI UART, and VirtIO console status.");
}
foreach (string project in new[] { cliProject, vsProject })
{
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Serial\\NovaOryn.Kernel.Serial.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Serial directly.");
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Ps2\\NovaOryn.Kernel.Ps2.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Ps2 directly.");
 Require(project.Contains("Sdk\\NovaOryn.String\\NovaOryn.String.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.String directly.");
}
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Serial","NovaOryn.Kernel.Serial.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Serial.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Serial","KernelSerial.cs")),"Generated SDK tree must contain KernelSerial.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Serial","SerialContracts.cs")),"Generated SDK tree must contain SerialContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Serial","SerialMath.cs")),"Generated SDK tree must contain SerialMath.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Ps2","NovaOryn.Kernel.Ps2.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Ps2.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Ps2","KernelPs2.cs")),"Generated SDK tree must contain KernelPs2.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Ps2","Ps2Contracts.cs")),"Generated SDK tree must contain Ps2Contracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Ps2","KeyboardLayouts.cs")),"Generated SDK tree must contain KeyboardLayouts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.String","NovaOryn.String.csproj")),"Generated SDK tree must contain NovaOryn.String.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.String","StringFormatter.cs")),"Generated SDK tree must contain StringFormatter.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Smp;",StringComparison.Ordinal)&&kernel.Contains("KernelSmp.Initialize(boot)",StringComparison.Ordinal),"Generated kernel must initialize the high-level SMP and per-CPU service.");
 Require(kernel.Contains("SMP and per-CPU state online.",StringComparison.Ordinal),"Generated kernel must visibly report SMP and per-CPU initialization.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Smp\\NovaOryn.Kernel.Smp.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Smp directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Smp","NovaOryn.Kernel.Smp.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Smp.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Smp","KernelSmp.cs")),"Generated SDK tree must contain KernelSmp.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Smp","KernelSmpContracts.cs")),"Generated SDK tree must contain KernelSmpContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Smp","KernelSmpMath.cs")),"Generated SDK tree must contain KernelSmpMath.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.Scheduler;",StringComparison.Ordinal)&&kernel.Contains("KernelScheduler.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the high-level scheduler and threads service.");
 Require(kernel.Contains("Scheduler and threads online.",StringComparison.Ordinal),"Generated kernel must visibly report scheduler initialization.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Scheduler\\NovaOryn.Kernel.Scheduler.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Scheduler directly.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Scheduler","NovaOryn.Kernel.Scheduler.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Scheduler.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Scheduler","KernelScheduler.cs")),"Generated SDK tree must contain KernelScheduler.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Scheduler","KernelSchedulerContracts.cs")),"Generated SDK tree must contain KernelSchedulerContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Scheduler","KernelSchedulerMath.cs")),"Generated SDK tree must contain KernelSchedulerMath.cs.");
}
foreach (string kernel in new[] { cliKernel, vsKernel })
{
 Require(kernel.Contains("using NovaOryn.Kernel.SystemCalls;",StringComparison.Ordinal)&&kernel.Contains("KernelSystemCalls.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the shared system-call core.");
 Require(kernel.Contains("using NovaOryn.Kernel.Processes;",StringComparison.Ordinal)&&kernel.Contains("KernelProcesses.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize processes and executable loading.");
 Require(kernel.Contains("using NovaOryn.Kernel.Drivers;",StringComparison.Ordinal)&&kernel.Contains("KernelDrivers.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize the driver framework.");
 Require(kernel.Contains("using NovaOryn.Kernel.Pci;",StringComparison.Ordinal)&&kernel.Contains("KernelPci.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize PCI/PCIe discovery after the driver framework.");
 Require(kernel.Contains("using NovaOryn.Kernel.Storage;",StringComparison.Ordinal)&&kernel.Contains("KernelStorage.Initialize()",StringComparison.Ordinal)&&!kernel.Contains("KernelFat32.Install()",StringComparison.Ordinal),"Generated kernel must initialize generic storage/VFS without baking in a filesystem provider.");
 Require(kernel.Contains("using NovaOryn.Kernel.Networking;",StringComparison.Ordinal)&&kernel.Contains("KernelNetworking.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize networking after storage.");
 Require(kernel.Contains("using NovaOryn.Kernel.Nvme;",StringComparison.Ordinal)&&kernel.Contains("KernelNvme.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize NVMe after storage.");
 Require(kernel.Contains("using NovaOryn.Kernel.Ahci;",StringComparison.Ordinal)&&kernel.Contains("KernelAhci.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize AHCI/SATA after storage.");
 Require(kernel.Contains("using NovaOryn.Kernel.Virtio;",StringComparison.Ordinal)&&kernel.Contains("KernelVirtio.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize VirtIO after storage and networking.");
 Require(kernel.Contains("using NovaOryn.Kernel.E1000;",StringComparison.Ordinal)&&kernel.Contains("KernelE1000.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize Intel E1000/E1000e after VirtIO-net.");
 Require(kernel.Contains("using NovaOryn.Kernel.Rtl8168;",StringComparison.Ordinal)&&kernel.Contains("KernelRtl8168.Initialize()",StringComparison.Ordinal),"Generated kernel must initialize Realtek RTL8168/RTL8111 after VirtIO-net.");
 Require(kernel.Contains("Get/Set/Event + Linux-style + NT-style",StringComparison.Ordinal),"Generated kernel must visibly report all three supported syscall methodologies.");
 Require(kernel.Contains("System calls online.",StringComparison.Ordinal),"Generated kernel must visibly report system-call initialization.");
 Require(kernel.Contains("Interactive console ready. Defaults: font 3, buffering 3. Userland: font get/set/list; buffering get/set/list.",StringComparison.Ordinal)&&kernel.Contains("KernelConsole.RunInteractive()",StringComparison.Ordinal),"Generated kernel must enter the interactive framebuffer console after boot.");
}
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.SystemCalls\\NovaOryn.Kernel.SystemCalls.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.SystemCalls directly.");
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Drivers\\NovaOryn.Kernel.Drivers.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Drivers directly.");
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Storage\\NovaOryn.Kernel.Storage.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Storage directly.");
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Networking\\NovaOryn.Kernel.Networking.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Networking directly.");
foreach (string project in new[] { cliProject, vsProject })
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Pci\\NovaOryn.Kernel.Pci.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Pci directly.");
foreach (string project in new[] { cliProject, vsProject })
{
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Nvme\\NovaOryn.Kernel.Nvme.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Nvme directly.");
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Ahci\\NovaOryn.Kernel.Ahci.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Ahci directly.");
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Virtio\\NovaOryn.Kernel.Virtio.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Virtio directly.");
 Require(project.Contains("Sdk\\NovaOryn.Kernel.E1000\\NovaOryn.Kernel.E1000.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.E1000 directly.");
 Require(project.Contains("Sdk\\NovaOryn.Kernel.Rtl8168\\NovaOryn.Kernel.Rtl8168.csproj",StringComparison.Ordinal),"Generated root project must reference NovaOryn.Kernel.Rtl8168 directly.");
}
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.SystemCalls","NovaOryn.Kernel.SystemCalls.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.SystemCalls.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.SystemCalls","KernelSystemCalls.cs")),"Generated SDK tree must contain KernelSystemCalls.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.SystemCalls","KernelSystemCallContracts.cs")),"Generated SDK tree must contain KernelSystemCallContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.SystemCalls","KernelSystemCallMath.cs")),"Generated SDK tree must contain KernelSystemCallMath.cs.");
}
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","NovaOryn.Kernel.Drivers.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Drivers.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","KernelDrivers.cs")),"Generated SDK tree must contain KernelDrivers.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","KernelDriverContracts.cs")),"Generated SDK tree must contain KernelDriverContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","KernelDriverMath.cs")),"Generated SDK tree must contain KernelDriverMath.cs.");
var driverProject = File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","NovaOryn.Kernel.Drivers.csproj"));
var driverSource = File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Drivers","KernelDrivers.cs"));
Require(driverProject.Contains("NovaOryn.Kernel.Heap",StringComparison.Ordinal),"Driver framework must reference the already-initialized kernel heap.");
Require(driverSource.Contains("KernelDriverFrameworkOptions.DynamicDefault",StringComparison.Ordinal)&&driverSource.Contains("KernelHeap.TryAllocate",StringComparison.Ordinal)&&driverSource.Contains("GrowDrivers",StringComparison.Ordinal)&&driverSource.Contains("GrowDevices",StringComparison.Ordinal),"Normal driver/device registries must grow dynamically from the kernel heap rather than enforce 64/128 hard limits.");
}

foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","NovaOryn.Kernel.Storage.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Storage.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","KernelStorage.cs")),"Generated SDK tree must contain KernelStorage.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","KernelVfs.cs")),"Generated SDK tree must contain KernelVfs.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","KernelStorageQueue.cs")),"Generated SDK tree must contain KernelStorageQueue.cs.");
 Require(!File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","KernelFat32.cs")),"Generated SDK tree must not contain the obsolete built-in FAT32 provider.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Networking","NovaOryn.Kernel.Networking.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Networking.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Networking","KernelNetworking.cs")),"Generated SDK tree must contain KernelNetworking.cs.");
 var storageProject=File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","NovaOryn.Kernel.Storage.csproj"));
 var storageSource=File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Storage","KernelStorage.cs"));
 Require(storageProject.Contains("NovaOryn.Kernel.Heap",StringComparison.Ordinal)&&storageProject.Contains("NovaOryn.Kernel.Drivers",StringComparison.Ordinal),"Storage must layer on the existing heap and driver framework.");
 Require(storageSource.Contains("KernelStorageOptions.DynamicDefault",StringComparison.Ordinal)&&storageSource.Contains("GrowDevices",StringComparison.Ordinal)&&storageSource.Contains("GrowVolumes",StringComparison.Ordinal),"Normal storage registries must grow dynamically rather than impose small hard limits.");
}
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Pci","NovaOryn.Kernel.Pci.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Pci.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Pci","KernelPci.cs")),"Generated SDK tree must contain KernelPci.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Pci","PciContracts.cs")),"Generated SDK tree must contain PciContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Pci","PciMath.cs")),"Generated SDK tree must contain PciMath.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Nvme","NovaOryn.Kernel.Nvme.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Nvme.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Ahci","NovaOryn.Kernel.Ahci.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Ahci.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Virtio","NovaOryn.Kernel.Virtio.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Virtio.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Virtio","KernelVirtio.cs")),"Generated SDK tree must contain KernelVirtio.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Virtio","VirtioContracts.cs")),"Generated SDK tree must contain VirtioContracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Virtio","VirtioMath.cs")),"Generated SDK tree must contain VirtioMath.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.E1000","NovaOryn.Kernel.E1000.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.E1000.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.E1000","KernelE1000.cs")),"Generated SDK tree must contain KernelE1000.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.E1000","E1000Contracts.cs")),"Generated SDK tree must contain E1000Contracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.E1000","E1000Math.cs")),"Generated SDK tree must contain E1000Math.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Rtl8168","NovaOryn.Kernel.Rtl8168.csproj")),"Generated SDK tree must contain NovaOryn.Kernel.Rtl8168.csproj.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Rtl8168","KernelRtl8168.cs")),"Generated SDK tree must contain KernelRtl8168.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Rtl8168","Rtl8168Contracts.cs")),"Generated SDK tree must contain Rtl8168Contracts.cs.");
 Require(File.Exists(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Rtl8168","Rtl8168Math.cs")),"Generated SDK tree must contain Rtl8168Math.cs.");
}
string projectCreator = Read(root,"src/NovaOryn.ProjectCreator/Program.cs");
Require(projectCreator.Contains("relative.StartsWith(\"Boot\" + Path.DirectorySeparatorChar",StringComparison.Ordinal)&&projectCreator.Contains("relative.StartsWith(\"HAL\" + Path.DirectorySeparatorChar",StringComparison.Ordinal)&&projectCreator.Contains("relative.StartsWith(\"KernelProjects\" + Path.DirectorySeparatorChar",StringComparison.Ordinal)&&projectCreator.Contains("relative.StartsWith(\"Userland\" + Path.DirectorySeparatorChar",StringComparison.Ordinal)&&projectCreator.Contains("relative.StartsWith(\"Tests\" + Path.DirectorySeparatorChar",StringComparison.Ordinal),"Selected-project refresh must preserve user-owned Boot, HAL, KernelProjects, Userland, and Tests source.");
Require(!projectCreator.Contains("new[] { \"Boot\", \"Console\", \"Runtime\" }",StringComparison.Ordinal),"Selected-project refresh must not delete the user-owned Boot tree.");
string synchronizer = Read(root,"src/NovaOryn.VisualStudio/NovaOrynSolutionSynchronizer.cs");
Require(synchronizer.Contains("QueueUserlandProjects",StringComparison.Ordinal)&&synchronizer.Contains("Task.Delay(500)",StringComparison.Ordinal)&&synchronizer.Contains("SwitchToMainThreadAsync",StringComparison.Ordinal),"Visual Studio workspace project synchronization must be deferred until after the New Project transaction rather than blocking project creation on the UI thread.");
Require(!synchronizer.Contains("Directory.EnumerateFiles(sdkDirectory",StringComparison.Ordinal)&&!synchronizer.Contains("Solution.AddFromFile(full",StringComparison.Ordinal),"Visual Studio project creation must not enumerate and load the full SDK project tree into the solution.");
Require(synchronizer.Contains("WorkspaceProjects.txt",StringComparison.Ordinal)&&synchronizer.Contains("EnsureConfiguredProjectsLoaded",StringComparison.Ordinal),"Visual Studio must load only projects present in the authoritative configured workspace graph.");
Require(synchronizer.Contains("RemoveInactiveGeneratedProjects",StringComparison.Ordinal)&&synchronizer.Contains("dte.Solution.Remove(project)",StringComparison.Ordinal),"Applying configuration must remove inactive generated projects from Solution Explorer without deleting their files.");
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 Require(File.Exists(Path.Combine(baseDir,"Userland","Commands","NovaOryn.Userland.Commands.csproj")),"Generated template must contain the Userland Commands project.");
 Require(File.Exists(Path.Combine(baseDir,"Userland","Commands","GeneralCommands.cs")),"Generated template must contain the standard userland command catalog.");
 Require(File.Exists(Path.Combine(baseDir,"Userland","Settings","NovaOryn.Userland.Settings.csproj")),"Generated template must contain the Userland Settings project.");
 Require(File.Exists(Path.Combine(baseDir,"Userland","Fonts","NovaOryn.Userland.Fonts.csproj")),"Generated template must contain the Userland Fonts project.");
 Require(File.Exists(Path.Combine(baseDir,"Userland","Images","NovaOryn.Userland.Images.csproj")),"Generated template must contain the Userland Images project.");
 Require(File.Exists(Path.Combine(baseDir,"Userland","Drivers","NovaOryn.Userland.Drivers.csproj")),"Generated template must contain the Userland Drivers project.");
}
string fatFsTemplate=Path.Combine(root,"src","NovaOryn.VisualStudio","ProjectTemplates","CSharp","1033","NovaOrynFilesystemFatFs");
Require(File.Exists(Path.Combine(fatFsTemplate,"FatFs.cs"))&&File.Exists(Path.Combine(fatFsTemplate,"LICENSE-FatFs.txt")),"FatFs project template must carry its selectable filesystem implementation and license.");
Require(File.ReadAllText(Path.Combine(fatFsTemplate,"Template.csproj")).Contains("<NovaOrynProjectType>KernelFileSystem</NovaOrynProjectType>",StringComparison.Ordinal),"FatFs project template must identify itself as a kernel filesystem project.");
Require(File.Exists(Path.Combine(vs,"NovaOryn.Configuration.json"))&&File.Exists(Path.Combine(vs,"NovaOryn.Configuration.props"))&&File.Exists(Path.Combine(vs,"NovaOryn.Configuration.targets")),"Visual Studio kernel template must carry configuration metadata plus generated MSBuild props/targets.");
Require(File.ReadAllText(Path.Combine(vs,"NovaOrynKernel.csproj")).Contains("NovaOryn.Configuration.props",StringComparison.Ordinal),"Visual Studio kernel project must import NovaOryn.Configuration.props.");
foreach (string templateName in new[] { "NovaOrynKernel", "NovaOrynKernelDriver", "NovaOrynKernelLibrary", "NovaOrynFilesystemFatFs", "NovaOrynUserlandApplication", "NovaOrynUserlandService", "NovaOrynUserlandDriver", "NovaOrynUserlandLibrary", "NovaOrynTestProject" })
{
 string templateDir=Path.Combine(root,"src","NovaOryn.VisualStudio","ProjectTemplates","CSharp","1033",templateName);
 Require(Directory.Exists(templateDir),$"Independent Visual Studio project template is missing: {templateName}");
 string[] vst=Directory.Exists(templateDir)?Directory.GetFiles(templateDir,"*.vstemplate",SearchOption.TopDirectoryOnly):[];
 Require(vst.Length==1,$"Independent Visual Studio project template must contain exactly one root .vstemplate: {templateName}");
 if(vst.Length==1)
 {
  string xml=File.ReadAllText(vst[0]);
  Require(xml.Contains("Type=\"Project\"",StringComparison.Ordinal),$"Independent Visual Studio template must be Type=Project: {templateName}");
  Require(!xml.Contains("Type=\"ProjectGroup\"",StringComparison.Ordinal),$"Independent Visual Studio template must not use ProjectGroup: {templateName}");
  Require(!xml.Contains("<Hidden>true</Hidden>",StringComparison.Ordinal),$"Independent Visual Studio template must be visible: {templateName}");
 }
}
Require(!cliProject.Contains("KernelProjects\\**\\*.csproj",StringComparison.Ordinal)&&!vsProject.Contains("KernelProjects\\**\\*.csproj",StringComparison.Ordinal),"Kernel templates must not use folder location as the architectural rule for kernel inclusion.");
Require(cliProject.Contains("@(NovaOrynConfiguredKernelProject)",StringComparison.Ordinal)&&vsProject.Contains("@(NovaOrynConfiguredKernelProject)",StringComparison.Ordinal),"Kernel templates must consume the configuration-generated kernel project graph.");
Require(File.Exists(Path.Combine(root,"templates","NovaOrynKernel","Build-WorkspaceProjects.ps1"))&&File.Exists(Path.Combine(vs,"Build-WorkspaceProjects.ps1")),"Kernel templates must include the independent Userland workspace build script.");
Require(File.Exists(Path.Combine(root,"templates","NovaOrynKernel","Tests","README.md"))&&File.Exists(Path.Combine(vs,"Tests","README.md")),"Kernel templates must include a Tests workspace root for independent test programs.");
Require(cliProject.Contains("Sdk\\NovaOryn.Kernel.Acpi\\NovaOryn.Kernel.Acpi.csproj",StringComparison.Ordinal)&&cliProject.Contains("Sdk\\NovaOryn.Kernel.Memory\\NovaOryn.Kernel.Memory.csproj",StringComparison.Ordinal)&&vsProject.Contains("Sdk\\NovaOryn.Kernel.Acpi\\NovaOryn.Kernel.Acpi.csproj",StringComparison.Ordinal)&&vsProject.Contains("Sdk\\NovaOryn.Kernel.Memory\\NovaOryn.Kernel.Memory.csproj",StringComparison.Ordinal),"Both root kernel templates must directly reference ACPI and physical-memory assemblies used by Kernel.cs.");
Finish();


string Read(string root, string relative) => File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
void Require(bool condition, string message) { if (!condition) failures.Add(message); }
void Finish()
{
    
foreach (string baseDir in new[] { Path.Combine(root,"templates","NovaOrynKernel"), vs })
{
 string generatedConsole=File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Console","KernelConsole.cs"));
 string generatedFramebuffer=File.ReadAllText(Path.Combine(baseDir,"Sdk","NovaOryn.Kernel.Console","FramebufferConsole.cs"));
 Require(string.Equals(generatedConsole,File.ReadAllText(Path.Combine(root,"src","NovaOryn.Kernel.Console","KernelConsole.cs")),StringComparison.Ordinal),"Generated KernelConsole.cs must match the authoritative SDK source.");
 Require(string.Equals(generatedFramebuffer,File.ReadAllText(Path.Combine(root,"src","NovaOryn.Kernel.Console","FramebufferConsole.cs")),StringComparison.Ordinal),"Generated FramebufferConsole.cs must match the authoritative SDK source.");
}
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
