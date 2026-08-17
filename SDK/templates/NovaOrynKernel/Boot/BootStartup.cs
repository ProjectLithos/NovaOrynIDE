using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Platform.X64;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.VirtualMemory;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.Time;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Protection;
using NovaOryn.Kernel.SystemCalls;
using NovaOryn.Kernel.Graphics;

namespace NovaOryn.Kernel.Bootstrap.Boot;

/// <summary>Owns boot-time platform, memory, scheduler, protection and syscall initialization.</summary>
public static unsafe class BootStartup
{
    /// <summary>Initializes NovaOryn through the protected kernel/runtime boundary before device discovery.</summary>
    public static Boolean Initialize(BootContext boot)
    {
        if (!KernelConsole.Initialize(boot)) return false;
        if (!KernelConsole.WriteLine("NovaOryn KMain started.")) return false;
        if (!boot.HasFinalMemoryMap()) return false;
        if (!KernelConsole.WriteLine("Final UEFI memory map retained; ExitBootServices succeeded.")) return false;
        if (!KernelPlatform.InitializeDescriptors()) return false;
        if (!KernelConsole.WriteLine("GDT and TSS installed.")) return false;
        if (!KernelPlatform.InitializeInterrupts()) return false;
        if (!KernelConsole.WriteLine("IDT with 256 vectors installed.")) return false;
        if (!KernelPlatform.DisableLegacyPic()) return false;
        if (!KernelConsole.WriteLine("Legacy PIC masked; APIC/MSI controller layer ready.")) return false;
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
        AcpiPowerCapabilities power = KernelAcpiPower.GetCapabilities();
        if (!KernelConsole.Write("ACPI FADT power reset/shutdown/button: ")) return false;
        if (!KernelConsole.Write(power.ResetAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.Write(power.ShutdownAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(power.PowerButtonAvailable ? "yes" : "no")) return false;
        if (!KernelConsole.Write("ACPI embedded controller: ")) return false;
        if (!KernelConsole.WriteLine(ecReady ? "ECDT online" : "not advertised by ECDT")) return false;
        if (!KernelConsole.WriteLine("ACPI MADT, MCFG, HPET, FADT and platform power services online.")) return false;
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
        if (!KernelConsole.WriteLine("HPET, Local APIC timer, TSC, RTC/CMOS and invariant-TSC clock source online.")) return false;
        if (!KernelPhysicalMemory.Initialize(boot)) return false;
        KernelPhysicalMemoryStatistics physicalMemory = KernelPhysicalMemory.GetStatistics();
        if (!KernelConsole.Write("Physical memory managed/free/reserved: ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.ManagedPages * 4096UL)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.FreePages * 4096UL)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteByteSize(physicalMemory.ReservedPages * 4096UL)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.WriteLine("Physical memory manager initialized from final UEFI map.")) return false;
        if (!KernelVirtualMemory.Initialize()) return false;
        if (!KernelConsole.WriteLine("Virtual memory manager attached to active x64 page tables.")) return false;
        Boolean addressSpaceReady = KernelAddressSpace.Initialize();
        if (!KernelConsole.Write("Kernel address-space status: ")) return false;
        if (!KernelConsole.WriteLine(KernelAddressSpace.GetLastStatusName())) return false;
        if (!addressSpaceReady)
        {
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
        if (!KernelConsole.Write("Kernel heap status: ")) return false;
        if (!KernelConsole.WriteLine(KernelHeap.GetLastStatusName())) return false;
        if (!heapReady) return false;
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
        if (!KernelConsole.WriteLine("SMP and per-CPU state online.")) return false;
        if (!KernelScheduler.Initialize()) return false;
        if (!KernelConsole.Write("Scheduler threads active: ")) return false;
        if (!KernelConsole.WriteUInt64(KernelScheduler.GetActiveThreadCount())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Scheduler quantum: ")) return false;
        if (!KernelConsole.WriteDurationNanoseconds(KernelScheduler.GetQuantumNanoseconds())) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("Timer preemption: ")) return false;
        if (!KernelConsole.WriteLine(KernelScheduler.GetCapabilities().HasTimerPreemption ? "available" : "cooperative only")) return false;
        if (!KernelConsole.WriteLine("Scheduler and threads online.")) return false;
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
        if (!KernelConsole.WriteLine("User/kernel separation online.")) return false;
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
        if (!KernelConsole.WriteLine("System calls online.")) return false;
        return true;
    }
    private static Int64 GetFontPresetSyscall(KernelSystemCallFrame* frame) => (Int64)KernelConsole.GetFontPreset();
    private static Int64 SetFontPresetSyscall(KernelSystemCallFrame* frame) => KernelConsole.SetFontPreset((UInt32)frame->Argument0) ? 0L : (Int64)KernelSystemCallError.InvalidArgument;
    private static Int64 GetBufferingPresetSyscall(KernelSystemCallFrame* frame) => (Int64)KernelConsole.GetFramebufferBufferSetting();
    private static Int64 SetBufferingPresetSyscall(KernelSystemCallFrame* frame) => KernelConsole.SetFramebufferBufferCount((UInt32)frame->Argument0) ? 0L : (Int64)KernelSystemCallError.InvalidArgument;

}
