using System;

namespace NovaOryn.Kernel.Smp;

/// <summary>Identifies the lifecycle state of one processor discovered for SMP.</summary>
public enum KernelProcessorStartupState : Byte
{
    /// <summary>The processor has been discovered but has not been started.</summary>
    Offline = 0,
    /// <summary>The processor is the bootstrap processor that entered from firmware.</summary>
    BootstrapProcessor = 1,
    /// <summary>An INIT/SIPI startup sequence is in progress.</summary>
    Starting = 2,
    /// <summary>The application processor reached the NovaOryn 64-bit trampoline and is safely parked.</summary>
    OnlineParked = 3,
    /// <summary>The processor cannot be started by the current xAPIC bootstrap mechanism.</summary>
    Unsupported = 4,
    /// <summary>The application processor did not complete its startup handshake.</summary>
    Failed = 5
}

/// <summary>Reports the overall result of SMP discovery and application-processor startup.</summary>
public enum KernelSmpStatus : Byte
{
    /// <summary>SMP and per-CPU state are not initialized.</summary>
    NotInitialized = 0,
    /// <summary>All discovered processors are represented and all startable processors are online.</summary>
    Success = 1,
    /// <summary>The bootstrap processor is online but one or more application processors could not be started.</summary>
    Partial = 2,
    /// <summary>ACPI did not advertise any enabled processors.</summary>
    NoProcessors = 3,
    /// <summary>The per-CPU state table could not be reserved.</summary>
    StateAllocationFailed = 4,
    /// <summary>The running bootstrap processor was not present in ACPI processor topology.</summary>
    BootstrapProcessorNotFound = 5,
    /// <summary>No valid low-memory AP startup trampoline was reserved before ExitBootServices.</summary>
    TrampolineUnavailable = 6,
    /// <summary>The Local APIC cannot be used by the current AP startup transport.</summary>
    LocalApicUnavailable = 7
}

/// <summary>Provides a stable snapshot of one processor's per-CPU bootstrap state.</summary>
public readonly struct KernelProcessorState
{
    /// <summary>Creates an immutable processor-state snapshot.</summary>
    public KernelProcessorState(UInt32 index, UInt32 apicId, UInt32 acpiUid, Boolean x2Apic, Boolean bootstrapProcessor, KernelProcessorStartupState startupState, UInt64 kernelStackBase, UInt64 kernelStackTop, UInt64 schedulerContext)
    { Index = index; ApicId = apicId; AcpiUid = acpiUid; IsX2Apic = x2Apic; IsBootstrapProcessor = bootstrapProcessor; StartupState = startupState; KernelStackBase = kernelStackBase; KernelStackTop = kernelStackTop; SchedulerContext = schedulerContext; }
    /// <summary>Gets the zero-based NovaOryn logical processor index.</summary>
    public UInt32 Index { get; }
    /// <summary>Gets the firmware-advertised Local APIC/x2APIC identifier.</summary>
    public UInt32 ApicId { get; }
    /// <summary>Gets the ACPI processor UID.</summary>
    public UInt32 AcpiUid { get; }
    /// <summary>Gets whether ACPI described this processor with an x2APIC entry.</summary>
    public Boolean IsX2Apic { get; }
    /// <summary>Gets whether this processor is the firmware bootstrap processor.</summary>
    public Boolean IsBootstrapProcessor { get; }
    /// <summary>Gets the current SMP bootstrap lifecycle state.</summary>
    public KernelProcessorStartupState StartupState { get; }
    /// <summary>Gets the NovaOryn-allocated AP bootstrap stack base, or zero for the BSP inherited stack.</summary>
    public UInt64 KernelStackBase { get; }
    /// <summary>Gets the exclusive AP bootstrap stack top, or zero for the BSP inherited stack.</summary>
    public UInt64 KernelStackTop { get; }
    /// <summary>Gets the scheduler-owned per-CPU context token reserved for roadmap item 15.</summary>
    public UInt64 SchedulerContext { get; }
}

/// <summary>Describes the initialized symmetric-multiprocessing environment.</summary>
public readonly struct KernelSmpCapabilities
{
    /// <summary>Creates an SMP capability snapshot.</summary>
    public KernelSmpCapabilities(UInt32 processors, UInt32 onlineProcessors, UInt32 bootstrapProcessorIndex, UInt64 trampolineAddress, Boolean xApicStartup)
    { ProcessorCount = processors; OnlineProcessorCount = onlineProcessors; BootstrapProcessorIndex = bootstrapProcessorIndex; TrampolineAddress = trampolineAddress; SupportsXApicStartup = xApicStartup; }
    /// <summary>Gets the number of enabled processors advertised by ACPI.</summary>
    public UInt32 ProcessorCount { get; }
    /// <summary>Gets the number of processors that completed NovaOryn bootstrap.</summary>
    public UInt32 OnlineProcessorCount { get; }
    /// <summary>Gets the logical index assigned to the firmware bootstrap processor.</summary>
    public UInt32 BootstrapProcessorIndex { get; }
    /// <summary>Gets the low-memory SIPI trampoline physical address reserved by UEFI.</summary>
    public UInt64 TrampolineAddress { get; }
    /// <summary>Gets whether xAPIC INIT/SIPI delivery is available for AP startup.</summary>
    public Boolean SupportsXApicStartup { get; }
}
