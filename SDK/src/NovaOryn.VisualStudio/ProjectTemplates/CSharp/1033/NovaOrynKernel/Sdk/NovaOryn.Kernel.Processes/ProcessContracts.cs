using System;

namespace NovaOryn.Kernel.Processes;

/// <summary>Identifies an executable image format understood by the NovaOryn process loader.</summary>
public enum ProcessExecutableFormat : Byte
{
    /// <summary>The image format is not recognized.</summary>
    Unknown = 0,
    /// <summary>System V ELF64 for x86-64.</summary>
    Elf64 = 1,
    /// <summary>Microsoft PE32+ for x86-64.</summary>
    PortableExecutable64 = 2
}

/// <summary>Identifies the lifecycle state of a user process.</summary>
public enum ProcessSegmentProtection : Byte
{
    /// <summary>The segment can be read.</summary>
    Read = 1,
    /// <summary>The segment can be written.</summary>
    Write = 2,
    /// <summary>The segment can be executed.</summary>
    Execute = 4
}

/// <summary>Identifies the lifecycle state of a user process.</summary>
public enum KernelProcessState : Byte
{
    /// <summary>The process-table slot is unused.</summary>
    Unused = 0,
    /// <summary>The image and address space are constructed but have not entered user mode.</summary>
    Ready = 1,
    /// <summary>The process has entered ring 3.</summary>
    Running = 2,
    /// <summary>The process was terminated and its owned image pages were released.</summary>
    Terminated = 3,
    /// <summary>Process startup failed after construction.</summary>
    Faulted = 4
}

/// <summary>Describes one loadable executable segment using ordinary .NET value semantics.</summary>
public readonly struct ProcessImageSegment
{
    /// <summary>Creates an immutable load-segment description.</summary>
    public ProcessImageSegment(UInt64 virtualAddress, UInt64 memorySize, UInt64 fileSize, UInt64 fileOffset, ProcessSegmentProtection protection)
    { VirtualAddress=virtualAddress; MemorySize=memorySize; FileSize=fileSize; FileOffset=fileOffset; Protection=protection; }
    public UInt64 VirtualAddress { get; }
    public UInt64 MemorySize { get; }
    public UInt64 FileSize { get; }
    public UInt64 FileOffset { get; }
    public ProcessSegmentProtection Protection { get; }
}

/// <summary>Reports validated executable metadata before any pages are allocated.</summary>
public readonly struct ProcessExecutableInfo
{
    /// <summary>Creates an executable-information snapshot.</summary>
    public ProcessExecutableInfo(ProcessExecutableFormat format, UInt64 entryPoint, UInt64 imageBase, UInt32 segmentCount, Boolean positionIndependent)
    { Format=format; EntryPoint=entryPoint; ImageBase=imageBase; SegmentCount=segmentCount; IsPositionIndependent=positionIndependent; }
    public ProcessExecutableFormat Format { get; }
    public UInt64 EntryPoint { get; }
    public UInt64 ImageBase { get; }
    public UInt32 SegmentCount { get; }
    public Boolean IsPositionIndependent { get; }
}

/// <summary>Provides an immutable public snapshot of one NovaOryn user process.</summary>
public readonly struct KernelProcessInfo
{
    /// <summary>Creates a process-information snapshot.</summary>
    public KernelProcessInfo(UInt64 id, KernelProcessState state, ProcessExecutableFormat format, UInt64 pageTableRoot, UInt64 entryPoint, UInt64 stackBase, UInt64 stackTop, Int64 exitCode)
    { Id=id; State=state; ExecutableFormat=format; PageTableRoot=pageTableRoot; EntryPoint=entryPoint; StackBase=stackBase; StackTop=stackTop; ExitCode=exitCode; }
    public UInt64 Id { get; }
    public KernelProcessState State { get; }
    public ProcessExecutableFormat ExecutableFormat { get; }
    public UInt64 PageTableRoot { get; }
    public UInt64 EntryPoint { get; }
    public UInt64 StackBase { get; }
    public UInt64 StackTop { get; }
    public Int64 ExitCode { get; }
}

/// <summary>Describes the bounded bootstrap process facility.</summary>
public readonly struct KernelProcessCapabilities
{
    /// <summary>Creates a process-capability snapshot.</summary>
    public KernelProcessCapabilities(UInt32 maximumProcesses, UInt32 activeProcesses, UInt32 maximumSegments, UInt64 defaultStackBytes, Boolean elf64, Boolean pe64)
    { MaximumProcesses=maximumProcesses; ActiveProcessCount=activeProcesses; MaximumSegments=maximumSegments; DefaultStackBytes=defaultStackBytes; SupportsElf64=elf64; SupportsPortableExecutable64=pe64; }
    public UInt32 MaximumProcesses { get; }
    public UInt32 ActiveProcessCount { get; }
    public UInt32 MaximumSegments { get; }
    public UInt64 DefaultStackBytes { get; }
    public Boolean SupportsElf64 { get; }
    public Boolean SupportsPortableExecutable64 { get; }
}
