using System;

namespace NovaOryn.Kernel.SystemCalls;

/// <summary>Identifies the syscall convention selected for one protected kernel entry.</summary>
public enum KernelSystemCallAbi : Byte
{
    Unknown = 0,
    GetSetEvent = 1,
    Linux = 2,
    Nt = 3
}

/// <summary>Identifies the three native NovaOryn service classes.</summary>
public enum KernelSystemCallOperation : Byte
{
    Get = 0,
    Set = 1,
    Event = 2
}

/// <summary>Defines stable negative NovaOryn/Linux-style syscall failures.</summary>
public enum KernelSystemCallError : Int64
{
    Success = 0,
    NotPermitted = -1,
    NotFound = -2,
    InvalidArgument = -22,
    NotImplemented = -38,
    Fault = -14,
    Busy = -16
}


/// <summary>Defines common NTSTATUS values used by the NT-style compatibility dispatcher.</summary>
public enum KernelNtStatus : UInt32
{
    Success = 0x00000000U,
    NotImplemented = 0xC0000002U,
    InvalidParameter = 0xC000000DU,
    AccessViolation = 0xC0000005U
}

/// <summary>Contains the six register arguments delivered to a NovaOryn syscall handler.</summary>
public unsafe struct KernelSystemCallFrame
{
    internal KernelSystemCallFrame(KernelSystemCallAbi abi, KernelSystemCallOperation operation, UInt32 service, UInt64 a0, UInt64 a1, UInt64 a2, UInt64 a3, UInt64 a4, UInt64 a5)
    { Abi=abi; Operation=operation; ServiceNumber=service; Argument0=a0; Argument1=a1; Argument2=a2; Argument3=a3; Argument4=a4; Argument5=a5; }
    public KernelSystemCallAbi Abi { get; }
    public KernelSystemCallOperation Operation { get; }
    public UInt32 ServiceNumber { get; }
    public UInt64 Argument0 { get; }
    public UInt64 Argument1 { get; }
    public UInt64 Argument2 { get; }
    public UInt64 Argument3 { get; }
    public UInt64 Argument4 { get; }
    public UInt64 Argument5 { get; }
}

/// <summary>Reports the active protected syscall environment.</summary>
public readonly struct KernelSystemCallCapabilities
{
    internal KernelSystemCallCapabilities(Boolean syscall, Boolean smap, UInt32 configured, UInt32 processors, UInt64 stackBytes)
    { HasX64Syscall=syscall; SmapEnabled=smap; ConfiguredProcessors=configured; ProcessorCount=processors; SyscallStackBytes=stackBytes; }
    public Boolean HasX64Syscall { get; }
    public Boolean SmapEnabled { get; }
    public UInt32 ConfiguredProcessors { get; }
    public UInt32 ProcessorCount { get; }
    public UInt64 SyscallStackBytes { get; }
    public Boolean SupportsGetSetEvent => true;
    public Boolean SupportsLinuxStyle => true;
    public Boolean SupportsNtStyle => true;
}
