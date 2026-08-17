using System;

namespace NovaOryn.Kernel.Scheduler;

/// <summary>Identifies the lifecycle state of a kernel thread.</summary>
public enum KernelThreadState : Byte
{
    /// <summary>The slot is unused.</summary>
    Unused = 0,
    /// <summary>The thread can be selected by a scheduler.</summary>
    Ready = 1,
    /// <summary>The thread is executing on a processor.</summary>
    Running = 2,
    /// <summary>The thread is blocked until explicitly awakened.</summary>
    Blocked = 3,
    /// <summary>The thread has completed and will not run again.</summary>
    Terminated = 4
}

/// <summary>Defines the four kernel scheduling priority bands.</summary>
public enum KernelThreadPriority : Byte
{
    /// <summary>Background work.</summary>
    Low = 0,
    /// <summary>Normal kernel work.</summary>
    Normal = 1,
    /// <summary>Latency-sensitive kernel work.</summary>
    High = 2,
    /// <summary>Critical kernel infrastructure.</summary>
    Critical = 3
}

/// <summary>Provides an immutable public snapshot of one kernel thread.</summary>
public readonly struct KernelThreadInfo
{
    /// <summary>Creates a thread-information snapshot.</summary>
    public KernelThreadInfo(UInt64 id, KernelThreadState state, KernelThreadPriority priority, UInt32 processor, UInt64 affinityMask, UInt64 stackBase, UInt64 stackTop, UInt64 entryPoint, UInt64 argument)
    { Id=id; State=state; Priority=priority; ProcessorIndex=processor; AffinityMask=affinityMask; StackBase=stackBase; StackTop=stackTop; EntryPoint=entryPoint; Argument=argument; }
    /// <summary>Gets the stable thread identifier.</summary>
    public UInt64 Id { get; }
    /// <summary>Gets the current lifecycle state.</summary>
    public KernelThreadState State { get; }
    /// <summary>Gets the scheduling priority.</summary>
    public KernelThreadPriority Priority { get; }
    /// <summary>Gets the processor currently owning the thread, or UInt32.MaxValue when not running.</summary>
    public UInt32 ProcessorIndex { get; }
    /// <summary>Gets the allowed-processor bit mask for logical processors 0 through 63.</summary>
    public UInt64 AffinityMask { get; }
    /// <summary>Gets the allocated kernel-stack base.</summary>
    public UInt64 StackBase { get; }
    /// <summary>Gets the exclusive kernel-stack top.</summary>
    public UInt64 StackTop { get; }
    /// <summary>Gets the initial x64 entry-point address.</summary>
    public UInt64 EntryPoint { get; }
    /// <summary>Gets the opaque argument supplied to the entry point.</summary>
    public UInt64 Argument { get; }
}

/// <summary>Describes scheduler capacity and current global state.</summary>
public readonly struct KernelSchedulerCapabilities
{
    /// <summary>Creates a scheduler-capability snapshot.</summary>
    public KernelSchedulerCapabilities(UInt32 processors, UInt32 maximumThreads, UInt32 activeThreads, UInt64 quantumNanoseconds, Boolean timerPreemption)
    { ProcessorCount=processors; MaximumThreads=maximumThreads; ActiveThreadCount=activeThreads; QuantumNanoseconds=quantumNanoseconds; HasTimerPreemption=timerPreemption; }
    /// <summary>Gets the number of processors represented by scheduler state.</summary>
    public UInt32 ProcessorCount { get; }
    /// <summary>Gets the fixed thread-table capacity.</summary>
    public UInt32 MaximumThreads { get; }
    /// <summary>Gets the number of non-unused thread records.</summary>
    public UInt32 ActiveThreadCount { get; }
    /// <summary>Gets the normal scheduler quantum in nanoseconds.</summary>
    public UInt64 QuantumNanoseconds { get; }
    /// <summary>Gets whether the Local APIC timer can provide preemption ticks.</summary>
    public Boolean HasTimerPreemption { get; }
}
