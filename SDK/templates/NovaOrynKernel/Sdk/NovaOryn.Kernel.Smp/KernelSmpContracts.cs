using System;

namespace NovaOryn.Kernel.Smp;

/// <summary>Identifies the lifecycle state of one processor discovered for SMP.</summary>
public enum KernelProcessorStartupState : Byte { Offline=0, BootstrapProcessor=1, Starting=2, OnlineParked=3, Unsupported=4, Failed=5, ShutdownRequested=6 }

/// <summary>Reports the overall result of SMP discovery and application-processor startup.</summary>
public enum KernelSmpStatus : Byte { NotInitialized=0, Success=1, Partial=2, NoProcessors=3, StateAllocationFailed=4, BootstrapProcessorNotFound=5, TrampolineUnavailable=6, LocalApicUnavailable=7 }

/// <summary>Standard inter-processor interrupt classes. Custom vectors remain available through TrySendIpi.</summary>
public enum KernelIpiPurpose : Byte { Reschedule=1, TlbShootdown=2, CallFunction=3, CpuShutdown=4, Diagnostic=5 }

/// <summary>Fixed per-CPU storage keys owned by the kernel. Subsystems may use User0-User3 for stable opaque tokens.</summary>
public enum KernelPerCpuStorageKey : Byte { Scheduler=0, Interrupt=1, Memory=2, Diagnostics=3, User0=4, User1=5, User2=6, User3=7 }

/// <summary>Immutable CPU enumeration record exposed by the formal SMP API.</summary>
public readonly struct KernelCpuInfo
{
 public KernelCpuInfo(UInt32 index,UInt32 apicId,UInt32 acpiUid,Boolean online,Boolean bootstrap,KernelProcessorStartupState state){Index=index;ApicId=apicId;AcpiUid=acpiUid;IsOnline=online;IsBootstrapProcessor=bootstrap;StartupState=state;}
 public UInt32 Index{get;} public UInt32 ApicId{get;} public UInt32 AcpiUid{get;} public Boolean IsOnline{get;} public Boolean IsBootstrapProcessor{get;} public KernelProcessorStartupState StartupState{get;}
}

/// <summary>Value object representing a processor-affinity mask for logical CPUs 0 through 63.</summary>
public readonly struct KernelCpuAffinity
{
 public KernelCpuAffinity(UInt64 mask){Mask=mask;} public UInt64 Mask{get;} public Boolean IsEmpty()=>Mask==0UL; public Boolean Allows(UInt32 cpu)=>cpu<64U && (Mask & (1UL<<(Int32)cpu))!=0UL; public static KernelCpuAffinity All()=>new(0xFFFFFFFFFFFFFFFFUL); public static KernelCpuAffinity Single(UInt32 cpu)=>cpu<64U?new(1UL<<(Int32)cpu):new(0UL);
}

/// <summary>Provides a stable snapshot of one processor's per-CPU bootstrap state.</summary>
public readonly struct KernelProcessorState
{
 public KernelProcessorState(UInt32 index,UInt32 apicId,UInt32 acpiUid,Boolean x2Apic,Boolean bootstrapProcessor,KernelProcessorStartupState startupState,UInt64 kernelStackBase,UInt64 kernelStackTop,UInt64 schedulerContext){Index=index;ApicId=apicId;AcpiUid=acpiUid;IsX2Apic=x2Apic;IsBootstrapProcessor=bootstrapProcessor;StartupState=startupState;KernelStackBase=kernelStackBase;KernelStackTop=kernelStackTop;SchedulerContext=schedulerContext;}
 public UInt32 Index{get;} public UInt32 ApicId{get;} public UInt32 AcpiUid{get;} public Boolean IsX2Apic{get;} public Boolean IsBootstrapProcessor{get;} public KernelProcessorStartupState StartupState{get;} public UInt64 KernelStackBase{get;} public UInt64 KernelStackTop{get;} public UInt64 SchedulerContext{get;}
}

/// <summary>Describes the initialized symmetric-multiprocessing environment and formal API capabilities.</summary>
public readonly struct KernelSmpCapabilities
{
 public KernelSmpCapabilities(UInt32 processors,UInt32 onlineProcessors,UInt32 bootstrapProcessorIndex,UInt64 trampolineAddress,Boolean xApicStartup,Boolean runtimeIpis,Boolean cpuStartup,Boolean cpuShutdown,UInt32 perCpuSlots){ProcessorCount=processors;OnlineProcessorCount=onlineProcessors;BootstrapProcessorIndex=bootstrapProcessorIndex;TrampolineAddress=trampolineAddress;SupportsXApicStartup=xApicStartup;SupportsRuntimeIpis=runtimeIpis;SupportsProcessorStartup=cpuStartup;SupportsProcessorShutdown=cpuShutdown;PerCpuStorageSlots=perCpuSlots;}
 public UInt32 ProcessorCount{get;} public UInt32 OnlineProcessorCount{get;} public UInt32 BootstrapProcessorIndex{get;} public UInt64 TrampolineAddress{get;} public Boolean SupportsXApicStartup{get;} public Boolean SupportsRuntimeIpis{get;} public Boolean SupportsProcessorStartup{get;} public Boolean SupportsProcessorShutdown{get;} public UInt32 PerCpuStorageSlots{get;}
}
