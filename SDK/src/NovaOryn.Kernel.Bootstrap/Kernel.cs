using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.CommandLine;
using NovaOryn.Kernel.Platform.X64;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.VirtualMemory;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.TimerDispatch;
using NovaOryn.Kernel.InterruptDispatch;
using NovaOryn.Kernel.InterruptBroker;
using NovaOryn.Kernel.Time;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Protection;
using NovaOryn.Kernel.SystemCalls;
using NovaOryn.Kernel.Ps2;
using NovaOryn.Kernel.Processes;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Storage;
using NovaOryn.Kernel.Networking;
using NovaOryn.Kernel.Pci;
using NovaOryn.Kernel.Nvme;
using NovaOryn.Kernel.Ahci;
using NovaOryn.Kernel.Virtio;
using NovaOryn.Kernel.Virtio.Gpu;
using NovaOryn.Kernel.Graphics;
using NovaOryn.Kernel.E1000;
using NovaOryn.Kernel.Rtl8168;
using NovaOryn.Bus.Usb;
using NovaOryn.Usb.Xhci;
using NovaOryn.Usb.Hid;
using NovaOryn.Usb.MassStorage;
using NovaOryn.Usb.Hub;

namespace NovaOryn.Kernel.Bootstrap;

/// <summary>Defines the authoritative freestanding NovaOryn bootstrap kernel.</summary>
public static unsafe class Kernel
{
    private const UInt64 KeyboardRepeatInitialDelayNanoseconds=300000000UL;
    private const UInt64 KeyboardRepeatIntervalNanoseconds=40000000UL;
    private static Boolean _ps2RepeatActive,_usbRepeatActive;
    private static Ps2Key _ps2RepeatKey;
    private static Char _ps2RepeatCharacter;
    private static UInt64 _ps2RepeatDeadline;
    private static Byte _usbRepeatUsage;
    private static Char _usbRepeatCharacter;
    private static UInt64 _usbRepeatDeadline;
    /// <summary>Initializes the kernel platform and enters the interrupt-driven interactive console.</summary>
    public static Boolean KMain(BootContext boot)
    {
        if (!KernelConsole.Initialize(boot)) return false;
        if (!KernelStructuredLogging.Initialize()) return false;
        if (!KernelPanicTransport.Initialize()) return false;
        if (!KernelStructuredLogging.TraceLine("console","Kernel.KMain","Kernel console initialized; structured diagnostic routing begins.")) return false;
        if (!KernelStructuredLogging.InfoLine("bootstrap","Kernel.KMain","NovaOryn KMain started.")) return false;
        if (!boot.HasFinalMemoryMap()) { KernelStructuredLogging.CriticalLine("boot","Kernel.KMain","Final UEFI memory map is missing; kernel startup cannot continue."); return false; }
        if (!KernelStructuredLogging.InfoLine("boot","Kernel.KMain","Final UEFI memory map retained; ExitBootServices succeeded.")) return false;
        if (!KernelPlatform.InitializeDescriptors()) return false;
        if (!KernelStructuredLogging.DebugLine("architecture","Kernel.KMain","GDT and TSS installed.")) return false;
        if (!KernelPlatform.InitializeInterrupts()) return false;
        if (!KernelStructuredLogging.DebugLine("interrupts","Kernel.KMain","IDT with 256 vectors installed.")) return false;
        if (!KernelPlatform.DisableLegacyPic()) return false;
        if (!KernelStructuredLogging.InfoLine("interrupts","Kernel.KMain","Legacy PIC masked; APIC/MSI controller layer ready.")) return false;
        if (!KernelAcpi.Initialize(boot)) return false;
        if (!KernelConsole.Write("ACPI status: ")) return false;
        if (!KernelConsole.WriteLine(KernelAcpi.GetLastStatusName())) return false;
        if (!KernelConsole.Write("ACPI RSDP: ")) return false;
        if (!KernelConsole.WriteHex(KernelAcpi.GetRootPointerAddress())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("ACPI root table: ")) return false;
        if (!KernelConsole.WriteHex(KernelAcpi.GetRootTableAddress())) return false;
        if (!KernelConsole.WriteLine(KernelAcpi.UsesXsdt() ? " XSDT" : " RSDT")) return false;
        if (!KernelConsole.Write("ACPI processors: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelAcpi.GetProcessorCount())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("ACPI I/O APICs: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelAcpi.GetIoApicCount())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (KernelAcpi.TryGetLocalApicAddress(out UInt64 localApicAddress))
        {
            if (!KernelConsole.Write("Local APIC base: ")) return false;
            if (!KernelConsole.WriteHex(localApicAddress)) return false;
            if (!KernelConsole.WriteLine("")) return false;
        }
        if (KernelAcpi.TryGetPciEcam(0U, out AcpiPciEcamInfo ecam))
        {
            if (!KernelConsole.Write("PCI ECAM base: ")) return false;
            if (!KernelConsole.WriteHex(ecam.BaseAddress)) return false;
            if (!KernelConsole.WriteLine("")) return false;
        }
        if (KernelAcpi.TryGetHpet(out AcpiHpetInfo hpet))
        {
            if (!KernelConsole.Write("HPET base: ")) return false;
            if (!KernelConsole.WriteHex(hpet.BaseAddress)) return false;
            if (!KernelConsole.WriteLine("")) return false;
        }
        if (!KernelAcpiFadt.Initialize()) return false;
        if (!KernelAcpiPower.Initialize()) return false;
        Boolean ecReady = KernelAcpiEc.Initialize();
        if (!ecReady) KernelStructuredLogging.WarningLine("acpi","Kernel.KMain","ACPI embedded controller was not advertised by ECDT; continuing without EC services.");
        AcpiPowerCapabilities power = KernelAcpiPower.GetCapabilities();
        if (!KernelConsole.Write("ACPI FADT power reset/shutdown/button: ")) return false;
        if (!KernelConsole.Write(power.ResetAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(power.ShutdownAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(power.PowerButtonAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write("ACPI embedded controller: ")) return false;
        if (!KernelConsole.WriteLine(ecReady ? "ECDT online" : "not advertised by ECDT")) return false;
        if (!KernelStructuredLogging.InfoLine("acpi","Kernel.KMain","ACPI MADT, MCFG, HPET, FADT and platform power services online.")) return false;
        if (!KernelTime.Initialize()) return false;
        KernelTimeCapabilities timeCapabilities = KernelTime.GetCapabilities();
        if (!KernelConsole.Write("HPET: ")) return false;
        if (!KernelConsole.Write(timeCapabilities.HasHpet ? "online @ " : "unavailable")) return false;
        if (timeCapabilities.HasHpet && !KernelConsole.WriteFrequency(KernelHpet.GetFrequencyHz())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("TSC: ")) return false;
        if (!KernelConsole.Write(timeCapabilities.HasTsc ? "available" : "unavailable")) return false;
        if (timeCapabilities.HasTsc)
        {
            if (!KernelConsole.Write(timeCapabilities.HasInvariantTsc ? " / invariant" : " / non-invariant")) return false;
            if (KernelTsc.GetFrequencyHz() != 0UL)
            {
                if (!KernelConsole.Write(" @ ")) return false;
                if (!KernelConsole.WriteFrequency(KernelTsc.GetFrequencyHz())) return false;
            }
        }
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Monotonic clock source: ")) return false;
        if (!KernelConsole.Write(KernelTime.GetClockSourceName())) return false;
        if (!KernelConsole.Write(" @ ")) return false;
        if (!KernelConsole.WriteFrequency(KernelTime.GetClockFrequencyHz())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Local APIC timer: ")) return false;
        if (!KernelConsole.WriteLine(KernelLocalApicTimer.IsAvailable() ? "calibrated" : "unavailable")) return false;
        if (KernelLocalApicTimer.IsAvailable())
        {
            if (!KernelConsole.Write("Local APIC timer frequency: ")) return false;
            if (!KernelConsole.WriteFrequency(KernelLocalApicTimer.GetFrequencyHz())) return false;
            if (!KernelConsole.WriteLine("")) return false;
        }
        if (!KernelConsole.Write("RTC/CMOS: ")) return false;
        if (KernelRtcCmos.TryRead(out KernelRtcDateTime rtc))
        {
            if (!KernelConsole.WriteUInt64(rtc.Year)) return false;
            if (!KernelConsole.Write("-")) return false;
            if (!KernelConsole.WriteUInt64(rtc.Month)) return false;
            if (!KernelConsole.Write("-")) return false;
            if (!KernelConsole.WriteUInt64(rtc.Day)) return false;
            if (!KernelConsole.Write(" ")) return false;
            if (!KernelConsole.WriteUInt64(rtc.Hour)) return false;
            if (!KernelConsole.Write(":")) return false;
            if (!KernelConsole.WriteUInt64(rtc.Minute)) return false;
            if (!KernelConsole.Write(":")) return false;
            if (!KernelConsole.WriteUInt64(rtc.Second)) return false;
            if (!KernelConsole.WriteLine("")) return false;
        }
        else if (!KernelConsole.WriteLine("unavailable")) return false;
        if (!KernelStructuredLogging.InfoLine("time","Kernel.KMain","HPET, Local APIC timer, TSC, RTC/CMOS and invariant-TSC clock source online.")) return false;
        if (!KernelPhysicalMemory.Initialize(boot)) return false;
        KernelPhysicalMemoryStatistics physicalMemory = KernelPhysicalMemory.GetStatistics();
        if (!KernelConsole.Write("Physical memory managed/free/reserved: ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.ManagedPages * 4096UL)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.FreePages * 4096UL)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.ReservedPages * 4096UL)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("memory","Kernel.KMain","Physical memory manager initialized from final UEFI map.")) return false;
        if (!KernelVirtualMemory.Initialize()) return false;
        if (!KernelStructuredLogging.InfoLine("virtual-memory","Kernel.KMain","Virtual memory manager attached to active x64 page tables.")) return false;
        Boolean addressSpaceReady = KernelAddressSpace.Initialize();
        if (!KernelConsole.Write("Kernel address-space status: ")) return false;
        if (!KernelConsole.WriteLine(KernelAddressSpace.GetLastStatusName())) return false;
        if (!addressSpaceReady)
        {
            KernelStructuredLogging.ErrorLine("virtual-memory","Kernel.KMain","Kernel address-space initialization failed.");
            if (!KernelConsole.Write("Virtual memory status: ")) return false;
            if (!KernelConsole.WriteLine(KernelVirtualMemory.GetLastStatusName())) return false;
            return false;
        }
        if (!KernelConsole.Write("Kernel image base: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelImageBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Kernel heap base: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelHeapBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Kernel stacks base: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelStacksBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Direct map base: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.DirectMapBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("MMIO base: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.MmioBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Page-table window: ")) return false;
        if (!KernelConsole.WriteHex(KernelAddressSpace.PageTableWindowBase)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelEarlyAllocator.Initialize()) return false;
        if (!KernelEarlyAllocator.TryAllocate(256UL, 16UL, out UInt64 earlyAddress)) return false;
        if (!KernelConsole.Write("Early allocator sample: ")) return false;
        if (!KernelConsole.WriteHex(earlyAddress)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        Boolean heapReady = KernelHeap.Initialize();
        if (heapReady)
        {
            if (!KernelTelemetry.ConfigureFreestanding(&KernelTelemetryTransport.TryEmit, &KernelTelemetryTransport.TryGetContext)) return false;
            KernelTelemetry.KernelBootEvent("Kernel heap", 8UL, KernelBootPhase.End, KernelHeap.GetLastStatusName());
            KernelTelemetry.KernelDiagnosticEvent("telemetry", "runtime-online", 0UL, "Structured kernel telemetry v1.1 online");
            if (!KernelStructuredLogging.InfoLine("logging","Kernel.KMain","Structured kernel logging is online with CPU/thread/process/time/source context.")) return false;
        }
        if (!KernelConsole.Write("Kernel heap status: ")) return false;
        if (!KernelConsole.WriteLine(KernelHeap.GetLastStatusName())) return false;
        if (!heapReady) { KernelStructuredLogging.CriticalLine("heap","Kernel.KMain","Kernel heap initialization failed; dynamic kernel services cannot start."); return false; }
        if (!KernelGraphics.Initialize()) return false;
        if (!FirmwareFramebuffer.Register(boot.GetFramebufferAddress(), boot.GetFramebufferSize(), boot.GetFramebufferWidth(), boot.GetFramebufferHeight(), boot.GetFramebufferPitchInPixels(), boot.GetFramebufferPixelFormat(), out KernelGraphicsDisplayHandle firmwareDisplay)) return false;
        if (!KernelConsole.Write("Generic framebuffer registered: ")) return false;
        if (!KernelConsole.WriteUInt64(firmwareDisplay.Value)) return false;
        if (!KernelConsole.Write(" @ ")) return false;
        if (!KernelConsole.WriteUInt64(boot.GetFramebufferWidth())) return false;
        if (!KernelConsole.Write("x")) return false;
        if (!KernelConsole.WriteUInt64(boot.GetFramebufferHeight())) return false;
        if (!KernelConsole.WriteLine(" (UEFI GOP generic framebuffer target).")) return false;
        UInt64 framebufferBufferBytes = KernelConsole.GetFramebufferBufferByteCount();
        if (framebufferBufferBytes == 0UL) return false;
        if (!KernelHeap.TryAllocate(framebufferBufferBytes, 4096UL, true, out KernelHeapAllocation framebufferBackBufferA)) return false;
        if (!KernelHeap.TryAllocate(framebufferBufferBytes, 4096UL, true, out KernelHeapAllocation framebufferBackBufferB)) return false;
        if (!KernelConsole.ConfigureFramebufferBuffers(framebufferBackBufferA.Address, framebufferBackBufferB.Address, framebufferBufferBytes)) return false;
        FramebufferBufferCapabilities framebufferBuffers = KernelConsole.GetFramebufferBufferCapabilities();
        if (!KernelConsole.Write("Framebuffer buffers available/active: ")) return false;
        if (!KernelConsole.WriteUInt64(framebufferBuffers.AvailableBufferCount)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64((UInt32)framebufferBuffers.Mode)) return false;
        if (!KernelConsole.WriteLine(" (Auto policy selects double buffering for text; GOP scan-out is the front buffer).")) return false;
        if (!KernelHeap.TryAllocate(256UL, 16UL, true, out KernelHeapAllocation heapSample)) return false;
        if (!KernelConsole.Write("Kernel heap sample: ")) return false;
        if (!KernelConsole.WriteHex(heapSample.Address)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelHeap.TryRelease(heapSample)) return false;
        if (!KernelSmp.Initialize(boot)) return false;
        if (!KernelConsole.Write("SMP status: ")) return false;
        if (!KernelConsole.WriteLine(KernelSmp.GetLastStatusName())) return false;
        if (!KernelConsole.Write("Processors online/total: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelSmp.GetOnlineProcessorCount())) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(KernelSmp.GetProcessorCount())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Bootstrap processor index: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelSmp.GetBootstrapProcessorIndex())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("AP trampoline: ")) return false;
        if (!KernelConsole.WriteHex(KernelSmp.GetCapabilities().TrampolineAddress)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","SMP and per-CPU state online.")) return false;
        KernelTelemetry.KernelBootEvent("SMP / per-CPU", 9UL, KernelBootPhase.End);
        KernelTelemetry.KernelCounter("smp", "processors-online", KernelSmp.GetOnlineProcessorCount());
        if (!KernelScheduler.Initialize()) return false;
        if (!KernelConsole.Write("Scheduler threads active: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelScheduler.GetActiveThreadCount())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Scheduler quantum: ")) return false;
        if (!KernelConsole.WriteDurationNanoseconds(KernelScheduler.GetQuantumNanoseconds())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Timer preemption: ")) return false;
        if (!KernelConsole.WriteLine(KernelScheduler.GetCapabilities().HasTimerPreemption ? "available" : "cooperative only")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Scheduler and threads online.")) return false;
        KernelTelemetry.KernelBootEvent("Scheduler", 10UL, KernelBootPhase.End);
        KernelTelemetry.KernelCounter("scheduler", "active-threads", KernelScheduler.GetActiveThreadCount());
        if (!KernelProtection.Initialize()) return false;
        KernelProtectionCapabilities protection = KernelProtection.GetCapabilities();
        if (!KernelConsole.Write("User range: ")) return false;
        if (!KernelConsole.WriteHex(protection.MinimumUserAddress)) return false;
        if (!KernelConsole.Write(" - ")) return false;
        if (!KernelConsole.WriteHex(protection.MaximumUserAddress)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Ring 3 selectors code/data: ")) return false;
        if (!KernelConsole.WriteHex(protection.UserCodeSelector)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteHex(protection.UserDataSelector)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Supervisor protections WP/SMEP/SMAP: ")) return false;
        if (!KernelConsole.Write(protection.WriteProtectEnabled ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(protection.SmepEnabled ? "on" : (protection.SmepSupported ? "available" : "unsupported"))) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(protection.SmapSupported ? "available for syscall copy guards" : "unsupported")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","User/kernel separation online.")) return false;
        KernelTelemetry.KernelBootEvent("Protection", 11UL, KernelBootPhase.End);
        if (!KernelSystemCalls.Initialize()) return false;
        if (!KernelSystemCalls.RegisterGet(33U, &GetFontPresetSyscall)) return false;
        if (!KernelSystemCalls.RegisterSet(33U, &SetFontPresetSyscall)) return false;
        if (!KernelSystemCalls.RegisterGet(34U, &GetBufferingPresetSyscall)) return false;
        if (!KernelSystemCalls.RegisterSet(34U, &SetBufferingPresetSyscall)) return false;
        KernelSystemCallCapabilities systemCalls = KernelSystemCalls.GetCapabilities();
        if (!KernelConsole.Write("System calls: Get/Set/Event + Linux-style + NT-style; syscall stack: ")) return false;
        if (!KernelConsole.WriteByteSize(systemCalls.SyscallStackBytes)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("SMAP guarded user copies: ")) return false;
        if (!KernelConsole.WriteLine(systemCalls.SmapEnabled ? "enabled" : "not supported")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","System calls online.")) return false;
        KernelTelemetry.KernelBootEvent("System calls", 12UL, KernelBootPhase.End);
        if (!KernelPs2.Initialize()) return false;
        if (!KernelPs2.SetKeyboardEventHandler(&HandleKeyboardEvent)) return false;
        Ps2Capabilities ps2 = KernelPs2.GetCapabilities();
        if (!KernelConsole.Write("PS/2 i8042 keyboard/mouse: ")) return false;
        if (!KernelConsole.Write(ps2.Controller ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(ps2.Keyboard ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(ps2.Mouse ? "on" : "off")) return false;
        if (!KernelConsole.Write("Keyboard layout: ")) return false;
        if (!KernelConsole.WriteLine(KeyboardLayouts.GetName(ps2.Layout))) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Keyboard layouts loaded: English_UK, English_USA.")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Keyboard repeat: software controlled; 300 ms delay, 40 ms interval; key-up cancels immediately.")) return false;
        if (!KernelProcesses.Initialize()) return false;
        KernelProcessCapabilities processes = KernelProcesses.GetCapabilities();
        if (!KernelConsole.Write("Processes ready/max: ")) return false;
        if (!KernelConsole.WriteUInt64(processes.ActiveProcessCount)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(processes.MaximumProcesses)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Executable loading: ELF64 + PE32+ x64; private user address spaces online.")) return false;
        if (!KernelInterruptDispatch.Initialize()) return false;
        if (!KernelTimerDispatch.Initialize()) return false;
        if (!KernelTimerDispatch.Register(1000000UL, &ServiceConsoleInput, 0UL, out _)) return false;
        if (!KernelTimerDispatch.Register(2000000UL, &ServiceNetworkAdapters, 0UL, out _)) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Interrupt and timer dispatch online; background polling disabled.")) return false;
        if (!KernelDrivers.Initialize()) return false;
        if (!KernelPci.Initialize()) return false;
        if (!KernelXhci.Initialize()) return false;
        if (!KernelXhci.ScanRootPorts()) return false;
        XhciCapabilities xhci = KernelXhci.GetCapabilities();
        if (!KernelConsole.Write("xHCI controllers/running/connected root ports: ")) return false;
        if (!KernelConsole.WriteUInt64(xhci.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(xhci.RunningControllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(xhci.ConnectedPorts)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelInterruptBroker.Initialize()) return false;
        Boolean keyboardIrq=KernelInterruptBroker.RegisterLegacyGsi(1U,false,false,&HandlePs2Interrupt,0UL,out _);
        Boolean mouseIrq=!ps2.Mouse||KernelInterruptBroker.RegisterLegacyGsi(12U,false,false,&HandlePs2Interrupt,0UL,out _);
        Boolean ps2HardwareIrqs=keyboardIrq&&mouseIrq&&KernelPs2.SetHardwareInterrupts(true);
        if (!KernelConsole.WriteLine(ps2HardwareIrqs ? "PS/2 interrupt delivery: hardware IRQs active." : "PS/2 interrupt delivery: timer-service fallback active.")) return false;
        KernelInterruptBrokerCapabilities interruptBroker = KernelInterruptBroker.GetCapabilities();
        if (!KernelConsole.Write("Interrupt broker Local APIC / I/O APIC / x2APIC / MSI / MSI-X: ")) return false;
        if (!KernelConsole.Write(interruptBroker.LocalApic ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(interruptBroker.IoApic ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(interruptBroker.X2Apic ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(interruptBroker.Msi ? "on" : "off")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(interruptBroker.MsiX ? "on" : "off")) return false;
        KernelDriverCapabilities drivers = KernelDrivers.GetCapabilities();
        PciCapabilities pci = KernelPci.GetCapabilities();
        if (!KernelConsole.Write("Driver framework drivers/devices: ")) return false;
        if (!KernelConsole.WriteUInt64(drivers.RegisteredDrivers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(drivers.RegisteredDevices)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("PCI/PCIe devices / ECAM segments: ")) return false;
        if (!KernelConsole.WriteUInt64(pci.DeviceCount)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(pci.EcamSegments)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","PCI configuration, BAR discovery, capabilities, MSI/MSI-X discovery and MMIO mapping online.")) return false;
        KernelTelemetry.KernelBootEvent("Drivers / PCI", 13UL, KernelBootPhase.End);
        KernelTelemetry.KernelTrace("driver", "pci-online", "PCI discovery and interrupt capabilities ready");
        if (!KernelConsole.WriteLine(drivers.RegistryMode == KernelDriverRegistryMode.Dynamic ? "Driver framework online; heap-backed registries grow dynamically." : "Driver framework online; fixed registry policy active.")) return false;
        if (!KernelStorage.Initialize()) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Filesystem providers: none selected by the base kernel; add the filesystem project(s) required by this OS.")) return false;
        if (!UsbHub.Initialize()) return false;
        if (!UsbHub.EnumerateDownstream()) return false;
        if (!UsbHid.Initialize()) return false;
        if (!UsbHid.SetKeyboardEventHandler(&HandleUsbKeyboardEvent)) return false;
        if (!UsbMassStorage.Initialize()) return false;
        UsbBusCapabilities usb = KernelUsbBus.GetCapabilities();
        UsbHubCapabilities usbHubs = UsbHub.GetCapabilities();
        UsbHidCapabilities usbHid = UsbHid.GetCapabilities();
        UsbMassStorageCapabilities usbStorage = UsbMassStorage.GetCapabilities();
        if (!KernelConsole.Write("USB hosts/devices/interfaces: ")) return false;
        if (!KernelConsole.WriteUInt64(usb.Hosts)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(usb.Devices)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(usb.Interfaces)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("USB hubs/ports, HID keyboards/mice, mass-storage devices: ")) return false;
        if (!KernelConsole.WriteUInt64(usbHubs.Hubs)) return false;
        if (!KernelConsole.Write("/")) return false;
        if (!KernelConsole.WriteUInt64(usbHubs.Ports)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(usbHid.Keyboards)) return false;
        if (!KernelConsole.Write("/")) return false;
        if (!KernelConsole.WriteUInt64(usbHid.Mice)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(usbStorage.Devices)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelNvme.Initialize()) return false;
        NvmeCapabilities nvme = KernelNvme.GetCapabilities();
        if (!KernelConsole.Write("NVMe controllers/namespaces: ")) return false;
        if (!KernelConsole.WriteUInt64(nvme.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(nvme.Namespaces)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelAhci.Initialize()) return false;
        AhciCapabilities ahci = KernelAhci.GetCapabilities();
        if (!KernelConsole.Write("AHCI controllers/SATA disks: ")) return false;
        if (!KernelConsole.WriteUInt64(ahci.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(ahci.Disks)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelNetworking.Initialize()) return false;
        if (!KernelVirtio.Initialize()) return false;
        if (!KernelVirtioGpu.Initialize()) return false;
        if (!KernelDrivers.BindAndStartMatchingDevices()) return false;
        if (!KernelE1000.Initialize()) return false;
        if (!KernelRtl8168.Initialize()) return false;
        VirtioCapabilities virtio = KernelVirtio.GetCapabilities();
        if (!KernelConsole.Write("VirtIO devices block/net/console/rng: ")) return false;
        if (!KernelConsole.WriteUInt64(virtio.BlockDevices)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(virtio.NetworkDevices)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(virtio.Consoles)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(virtio.EntropySources)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        VirtioGpuCapabilities virtioGpu = KernelVirtioGpu.GetCapabilities();
        if (!KernelConsole.Write("VirtIO GPU controllers/displays: ")) return false;
        if (!KernelConsole.WriteUInt64(virtioGpu.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(virtioGpu.Displays)) return false;
        if (!KernelConsole.WriteLine(" (2D resources + mode changes).")) return false;
        KernelGraphicsCapabilities graphics = KernelGraphics.GetCapabilities();
        if (!KernelConsole.Write("Graphics displays firmware/simple/VirtIO/total: ")) return false;
        if (!KernelConsole.WriteUInt64(graphics.FirmwareFramebuffers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(graphics.SimpleFramebuffers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(graphics.VirtioGpus)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(graphics.Displays)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        E1000Capabilities e1000 = KernelE1000.GetCapabilities();
        if (!KernelConsole.Write("Intel E1000/E1000e controllers/interfaces: ")) return false;
        if (!KernelConsole.WriteUInt64(e1000.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(e1000.Interfaces)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        Rtl8168Capabilities rtl8168 = KernelRtl8168.GetCapabilities();
        if (!KernelConsole.Write("Realtek RTL8168/RTL8111 controllers/interfaces: ")) return false;
        if (!KernelConsole.WriteUInt64(rtl8168.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(rtl8168.Interfaces)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        KernelStorageCapabilities storage = KernelStorage.GetCapabilities();
        if (!KernelConsole.Write("Storage devices/volumes/mounts: ")) return false;
        if (!KernelConsole.WriteUInt64(storage.Devices)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(storage.Volumes)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(storage.Mounts)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Storage/VFS online; MBR + GPT discovery, FAT32 and VirtIO block ready.")) return false;
        KernelTelemetry.KernelBootEvent("Storage / VFS", 14UL, KernelBootPhase.End);
        KernelTelemetry.KernelTrace("storage", "storage-online");
        KernelNetworkCapabilities networking = KernelNetworking.GetCapabilities();
        if (!KernelConsole.Write("Networking interfaces/routes/sockets: ")) return false;
        if (!KernelConsole.WriteUInt64(networking.Interfaces)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(networking.Routes)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(networking.Sockets)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Networking online; Ethernet + ARP + IPv4 + ICMP + UDP + TCP with VirtIO-net, Intel E1000/E1000e and Realtek RTL8168/RTL8111 ready.")) return false;
        KernelTelemetry.KernelBootEvent("Networking", 15UL, KernelBootPhase.End);
        KernelTelemetry.KernelTrace("network", "network-stack-online");
        if (!KernelSubsystemRuntime.ValidateAll(out UInt32 readySubsystems,out UInt32 degradedSubsystems)) return false;
        if (!KernelConsole.Write("Formal subsystem contracts 1.0 active; ready/degraded: ")) return false;
        if (!KernelConsole.WriteUInt64(readySubsystems)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(degradedSubsystems)) return false;
        if (!KernelConsole.WriteLine(". Kernel runtime is gated by the public subsystem boundaries.")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Capability policy active: driver declarations are privilege ceilings; bound devices run only with kernel-issued grants.")) return false;
        if (!KernelStructuredLogging.InfoLine("kernel","Kernel.KMain","Interactive console ready. Defaults: font 3, buffering auto (double for text).")) return false;
        KernelTelemetry.KernelBootEvent("Interactive console", 16UL, KernelBootPhase.End);
        KernelTelemetry.KernelProfile("boot", "KMain-postheap", 1UL, KernelTime.GetMonotonicNanoseconds());
        if (!KernelCommandLine.Initialize()) return false;
        if (!KernelInterruptDispatch.Enable()) return false;
        return KernelConsole.RunInteractive();
    }
    private static Int64 GetFontPresetSyscall(KernelSystemCallFrame* frame) => (Int64)KernelConsole.GetFontPreset();
    private static Int64 SetFontPresetSyscall(KernelSystemCallFrame* frame) => KernelConsole.SetFontPreset((UInt32)frame->Argument0) ? 0L : (Int64)KernelSystemCallError.InvalidArgument;
    private static Int64 GetBufferingPresetSyscall(KernelSystemCallFrame* frame) => (Int64)KernelConsole.GetFramebufferBufferSetting();
    private static Int64 SetBufferingPresetSyscall(KernelSystemCallFrame* frame) => KernelConsole.SetFramebufferBufferCount((UInt32)frame->Argument0) ? 0L : (Int64)KernelSystemCallError.InvalidArgument;

    private static Boolean ServiceConsoleInput(UInt64 cookie)
    {
        // USB HID currently uses interrupt endpoints serviced by the timer dispatcher.
        // PS/2 is hardware-IRQ driven; Service remains harmless as a drain fallback.
        Boolean ok=KernelConsole.TickCaret();
        // Drain physical transitions before repeat work. A matching key-up therefore
        // cancels repeat before this service tick can emit another repeated action.
        ok=KernelPs2.Service()&ok;
        if (UsbHid.IsInitialized()) ok=UsbHid.Service()&ok;
        ok=ServiceKeyboardRepeat()&ok;
        return ok;
    }
    private static Boolean HandlePs2Interrupt(Byte vector,UInt64 cookie) => KernelPs2.Service();
    // Decoded PS/2 events are consumed here; KernelConsole never rereads i8042 hardware.
    private static Boolean HandleKeyboardEvent(Ps2KeyboardEvent input)
    {
        if (!input.Pressed)
        {
            if (_ps2RepeatActive && _ps2RepeatKey==input.Key) _ps2RepeatActive=false;
            return true;
        }

        Boolean ok=DispatchPs2Press(input);
        if (ok && IsPs2Repeatable(input))
        {
            _ps2RepeatActive=true;
            _ps2RepeatKey=input.Key;
            _ps2RepeatCharacter=input.Character;
            _ps2RepeatDeadline=NextRepeatDeadline(KeyboardRepeatInitialDelayNanoseconds);
        }
        return ok;
    }
    private static Boolean HandleUsbKeyboardEvent(UsbHidKeyboardEvent input)
    {
        if (!input.Pressed)
        {
            if (_usbRepeatActive && _usbRepeatUsage==input.Usage) _usbRepeatActive=false;
            return true;
        }

        Boolean ok=DispatchUsbPress(input);
        if (ok && IsUsbRepeatable(input))
        {
            _usbRepeatActive=true;
            _usbRepeatUsage=input.Usage;
            _usbRepeatCharacter=input.Character;
            _usbRepeatDeadline=NextRepeatDeadline(KeyboardRepeatInitialDelayNanoseconds);
        }
        return ok;
    }
    private static Boolean DispatchPs2Press(Ps2KeyboardEvent input)
    {
        if (input.Key == Ps2Key.Up) return KernelConsole.ScrollUp();
        if (input.Key == Ps2Key.Down) return KernelConsole.ScrollDown();
        if (input.Control && input.Key == Ps2Key.D1) return KernelConsole.SetFramebufferBufferCount(1U);
        if (input.Control && input.Key == Ps2Key.D2) return KernelConsole.SetFramebufferBufferCount(2U);
        if (input.Control && input.Key == Ps2Key.D3) return KernelConsole.SetFramebufferBufferCount(3U);
        if (input.Alt && input.Key == Ps2Key.D1) return KernelConsole.SetFontPreset(1U);
        if (input.Alt && input.Key == Ps2Key.D2) return KernelConsole.SetFontPreset(2U);
        if (input.Alt && input.Key == Ps2Key.D3) return KernelConsole.SetFontPreset(3U);
        return KernelCommandLine.HandleCharacter(input.Character);
    }
    private static Boolean DispatchUsbPress(UsbHidKeyboardEvent input)
    {
        if (input.Usage == 82U) return KernelConsole.ScrollUp();
        if (input.Usage == 81U) return KernelConsole.ScrollDown();
        Boolean control=(input.Modifiers&0x11U)!=0;
        Boolean alt=(input.Modifiers&0x44U)!=0;
        if (control && input.Usage == 30U) return KernelConsole.SetFramebufferBufferCount(1U);
        if (control && input.Usage == 31U) return KernelConsole.SetFramebufferBufferCount(2U);
        if (control && input.Usage == 32U) return KernelConsole.SetFramebufferBufferCount(3U);
        if (alt && input.Usage == 30U) return KernelConsole.SetFontPreset(1U);
        if (alt && input.Usage == 31U) return KernelConsole.SetFontPreset(2U);
        if (alt && input.Usage == 32U) return KernelConsole.SetFontPreset(3U);
        return KernelCommandLine.HandleCharacter(input.Character);
    }
    private static Boolean IsPs2Repeatable(Ps2KeyboardEvent input)
    {
        if(input.Control||input.Alt)return false;
        return input.Key==Ps2Key.Up||input.Key==Ps2Key.Down||IsRepeatableCharacter(input.Character);
    }
    private static Boolean IsUsbRepeatable(UsbHidKeyboardEvent input)
    {
        if((input.Modifiers&0x55U)!=0U)return false;
        return input.Usage==82U||input.Usage==81U||IsRepeatableCharacter(input.Character);
    }
    private static Boolean IsRepeatableCharacter(Char character)=>character=='\b'||(character>=' '&&character<='~');
    private static UInt64 NextRepeatDeadline(UInt64 delay)
    {
        UInt64 now=KernelTime.GetMonotonicNanoseconds();
        return UInt64.MaxValue-now<delay?UInt64.MaxValue:now+delay;
    }
    private static Boolean ServiceKeyboardRepeat()
    {
        UInt64 now=KernelTime.GetMonotonicNanoseconds();
        Boolean ok=true;

        if(_ps2RepeatActive&&now>=_ps2RepeatDeadline)
        {
            // Schedule from now rather than catching up missed periods. Slow framebuffer
            // work cannot accumulate queued repeat actions that continue after key-up.
            _ps2RepeatDeadline=UInt64.MaxValue-now<KeyboardRepeatIntervalNanoseconds?UInt64.MaxValue:now+KeyboardRepeatIntervalNanoseconds;
            Boolean repeated=_ps2RepeatKey==Ps2Key.Up?KernelConsole.ScrollUp():
                _ps2RepeatKey==Ps2Key.Down?KernelConsole.ScrollDown():
                KernelCommandLine.HandleCharacter(_ps2RepeatCharacter);
            if(!repeated){_ps2RepeatActive=false;ok=false;}
        }

        if(_usbRepeatActive&&now>=_usbRepeatDeadline)
        {
            _usbRepeatDeadline=UInt64.MaxValue-now<KeyboardRepeatIntervalNanoseconds?UInt64.MaxValue:now+KeyboardRepeatIntervalNanoseconds;
            Boolean repeated=_usbRepeatUsage==82U?KernelConsole.ScrollUp():
                _usbRepeatUsage==81U?KernelConsole.ScrollDown():
                KernelCommandLine.HandleCharacter(_usbRepeatCharacter);
            if(!repeated){_usbRepeatActive=false;ok=false;}
        }

        return ok;
    }
    private static Boolean ServiceNetworkAdapters(UInt64 cookie)
    {
        Boolean ok=true;
        if (KernelVirtio.IsInitialized()) ok=KernelVirtio.ServiceAll()&ok;
        if (KernelE1000.IsInitialized()) ok=KernelE1000.ServiceAll()&ok;
        if (KernelRtl8168.IsInitialized()) ok=KernelRtl8168.ServiceAll()&ok;
        return ok;
    }

}
