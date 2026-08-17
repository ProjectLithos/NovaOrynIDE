using System;

namespace NovaOryn.Kernel.SystemCalls;

/// <summary>Provides stable encoding and validation rules for NovaOryn syscall ABI namespaces.</summary>
public static class KernelSystemCallMath
{
    public const UInt64 NamespaceMask = 0xFFFF000000000000UL;
    public const UInt64 GetSetEventNamespace = 0x4E4F000000000000UL;
    public const UInt64 LinuxNamespace = 0x4C58000000000000UL;
    public const UInt64 NtNamespace = 0x4E54000000000000UL;
    public const UInt64 ServiceMask = 0x00000000FFFFFFFFUL;
    public const UInt32 MaximumRegisteredService = 63U;

    /// <summary>Encodes one NovaOryn Get/Set/Event operation for the x64 SYSCALL entry.</summary>
    public static UInt64 EncodeGetSetEvent(KernelSystemCallOperation operation, UInt32 service)
    {
        return GetSetEventNamespace | ((UInt64)operation << 32) | service;
    }

    /// <summary>Encodes one Linux-style syscall number without changing the original numeric ID.</summary>
    public static UInt64 EncodeLinux(UInt32 syscallNumber) => LinuxNamespace | syscallNumber;

    /// <summary>Encodes one NT-style service number without assuming a Windows-version-specific table.</summary>
    public static UInt64 EncodeNt(UInt32 serviceNumber) => NtNamespace | serviceNumber;

    /// <summary>Attempts to decode the ABI namespace carried by an encoded x64 syscall number.</summary>
    public static Boolean TryDecodeAbi(UInt64 encoded, out KernelSystemCallAbi abi)
    {
        UInt64 ns = encoded & NamespaceMask;
        if (ns == GetSetEventNamespace) { abi = KernelSystemCallAbi.GetSetEvent; return true; }
        if (ns == LinuxNamespace) { abi = KernelSystemCallAbi.Linux; return true; }
        if (ns == NtNamespace) { abi = KernelSystemCallAbi.Nt; return true; }
        abi = KernelSystemCallAbi.Unknown;
        return false;
    }

    /// <summary>Gets the 32-bit service number carried by any supported syscall namespace.</summary>
    public static UInt32 GetServiceNumber(UInt64 encoded) => (UInt32)(encoded & ServiceMask);

    /// <summary>Gets the Get/Set/Event operation class encoded above the service ID.</summary>
    public static KernelSystemCallOperation GetOperation(UInt64 encoded) => (KernelSystemCallOperation)((encoded >> 32) & 0xFFUL);

    /// <summary>Determines whether a custom service ID fits the bounded freestanding registry.</summary>
    public static Boolean IsRegistrableService(UInt32 service) => service <= MaximumRegisteredService;
}
