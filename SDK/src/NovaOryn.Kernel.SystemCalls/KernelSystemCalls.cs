using System;
using System.Runtime;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.Protection;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Time;
using NovaOryn.Kernel.VirtualMemory;

namespace NovaOryn.Kernel.SystemCalls;

/// <summary>Provides the shared protected syscall core for NovaOryn, Linux-style, and NT-style services.</summary>
public static unsafe class KernelSystemCalls
{
    private const UInt64 SyscallStackBytes = 32768UL;
    private const Int32 RegistrySlots = 64;

    private unsafe struct Registry
    {
        internal fixed UInt64 Get[RegistrySlots];
        internal fixed UInt64 Set[RegistrySlots];
        internal fixed UInt64 Event[RegistrySlots];
        internal fixed UInt64 Linux[RegistrySlots];
        internal fixed UInt64 Nt[RegistrySlots];
    }

#pragma warning disable CS0169
    private static Registry _registry;
#pragma warning restore CS0169
    private static Boolean _initialized, _smapEnabled;
    private static UInt64 _stateAddress, _stackBase, _stackTop;
    private static UInt32 _configuredProcessors;

    /// <summary>Initializes the x64 SYSCALL/SYSRET boundary for the currently executing bootstrap processor.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelProtection.IsInitialized() || !KernelHeap.IsInitialized() || !KernelSmp.IsInitialized()) return false;
        if (!KernelHeap.TryAllocate(SyscallStackBytes, 16UL, true, out KernelHeapAllocation stack)) return false;
        if (!KernelHeap.TryAllocate(128UL, 16UL, true, out KernelHeapAllocation state)) return false;
        _stackBase = stack.Address;
        _stackTop = stack.Address + stack.ByteCount;
        _stateAddress = state.Address;
        if (!Native.ConfigureSystemCalls(_stateAddress, _stackTop)) return false;
        _configuredProcessors = 1U;
        KernelProtectionCapabilities protection = KernelProtection.GetCapabilities();
        if (protection.SmapSupported)
        {
            if (!Native.EnableSmap()) return false;
            _smapEnabled = Native.IsSmapEnabled();
            if (!_smapEnabled) return false;
        }
        _initialized = true;
        return true;
    }

    public static Boolean IsInitialized() => _initialized;
    public static KernelSystemCallCapabilities GetCapabilities() => new(_initialized, _smapEnabled, _configuredProcessors, KernelSmp.GetProcessorCount(), SyscallStackBytes);
    public static UInt64 GetSyscallStackBase() => _stackBase;
    public static UInt64 GetSyscallStackTop() => _stackTop;

    /// <summary>Registers a custom NovaOryn Get handler using a normal managed function pointer with no delegate allocation.</summary>
    public static Boolean RegisterGet(UInt32 service, delegate*<KernelSystemCallFrame*, Int64> handler) => Register(KernelSystemCallOperation.Get, service, handler);
    /// <summary>Registers a custom NovaOryn Set handler.</summary>
    public static Boolean RegisterSet(UInt32 service, delegate*<KernelSystemCallFrame*, Int64> handler) => Register(KernelSystemCallOperation.Set, service, handler);
    /// <summary>Registers a custom NovaOryn Event handler.</summary>
    public static Boolean RegisterEvent(UInt32 service, delegate*<KernelSystemCallFrame*, Int64> handler) => Register(KernelSystemCallOperation.Event, service, handler);
    /// <summary>Registers a Linux-style numeric syscall handler in the bounded bootstrap table.</summary>
    public static Boolean RegisterLinux(UInt32 syscallNumber, delegate*<KernelSystemCallFrame*, Int64> handler) => RegisterAbi(KernelSystemCallAbi.Linux, syscallNumber, handler);
    /// <summary>Registers an NT-style numeric service handler in the bounded bootstrap table.</summary>
    public static Boolean RegisterNt(UInt32 serviceNumber, delegate*<KernelSystemCallFrame*, Int64> handler) => RegisterAbi(KernelSystemCallAbi.Nt, serviceNumber, handler);

    /// <summary>Copies bytes from a validated readable user range while honoring SMAP when enabled.</summary>
    public static Boolean TryCopyFromUser(UInt64 userSource, UInt64 kernelDestination, UInt64 byteCount)
    {
        if (!_initialized || kernelDestination == 0UL || !ValidateUserRange(userSource, byteCount, false)) return false;
        if (!Native.BeginUserMemoryAccess()) return false;
        Byte* src=(Byte*)userSource; Byte* dst=(Byte*)kernelDestination;
        for (UInt64 i=0UL;i<byteCount;i++) dst[i]=src[i];
        return Native.EndUserMemoryAccess();
    }

    /// <summary>Copies bytes into a validated writable user range while honoring SMAP when enabled.</summary>
    public static Boolean TryCopyToUser(UInt64 userDestination, UInt64 kernelSource, UInt64 byteCount)
    {
        if (!_initialized || kernelSource == 0UL || !ValidateUserRange(userDestination, byteCount, true)) return false;
        if (!Native.BeginUserMemoryAccess()) return false;
        Byte* src=(Byte*)kernelSource; Byte* dst=(Byte*)userDestination;
        for (UInt64 i=0UL;i<byteCount;i++) dst[i]=src[i];
        return Native.EndUserMemoryAccess();
    }

    /// <summary>Dispatches one encoded syscall through the selected ABI namespace.</summary>
    public static Int64 Dispatch(UInt64 encoded, UInt64 a0, UInt64 a1, UInt64 a2, UInt64 a3, UInt64 a4, UInt64 a5)
    {
        if (!_initialized || !KernelSystemCallMath.TryDecodeAbi(encoded, out KernelSystemCallAbi abi)) return (Int64)KernelSystemCallError.NotImplemented;
        UInt32 service=KernelSystemCallMath.GetServiceNumber(encoded);
        KernelSystemCallOperation operation=abi==KernelSystemCallAbi.GetSetEvent ? KernelSystemCallMath.GetOperation(encoded) : KernelSystemCallOperation.Get;
        KernelSystemCallFrame frame=new(abi,operation,service,a0,a1,a2,a3,a4,a5);
        if (abi==KernelSystemCallAbi.GetSetEvent) return DispatchNative(&frame);
        if (abi==KernelSystemCallAbi.Linux) return DispatchLinux(&frame);
        return DispatchNt(&frame);
    }

    [RuntimeExport("NovaOrynManagedSyscallDispatch")]
    private static Int64 NativeDispatch(UInt64 encoded, UInt64 a0, UInt64 a1, UInt64 a2, UInt64 a3, UInt64 a4, UInt64 a5) => Dispatch(encoded,a0,a1,a2,a3,a4,a5);

    private static Int64 DispatchNative(KernelSystemCallFrame* frame)
    {
        if (frame->Operation==KernelSystemCallOperation.Get)
        {
            if (frame->ServiceNumber==0U) return 1L;
            if (frame->ServiceNumber==1U) return unchecked((Int64)KernelTime.GetMonotonicNanoseconds());
            if (frame->ServiceNumber==2U) return KernelSmp.GetOnlineProcessorCount();
        }
        else if (frame->Operation==KernelSystemCallOperation.Set)
        {
            if (frame->ServiceNumber==0U) return KernelScheduler.SetQuantumNanoseconds(frame->Argument0) ? 0L : (Int64)KernelSystemCallError.InvalidArgument;
        }
        else if (frame->Operation==KernelSystemCallOperation.Event)
        {
            if (frame->ServiceNumber==0U && KernelSmp.TryGetCurrentProcessor(out KernelProcessorState cpu))
                return KernelScheduler.Yield(cpu.Index,out UInt64 _) ? 0L : (Int64)KernelSystemCallError.Busy;
        }
        return DispatchRegistered(frame->Operation,frame);
    }

    private static Int64 DispatchLinux(KernelSystemCallFrame* frame)
    {
        if (frame->ServiceNumber==24U && KernelSmp.TryGetCurrentProcessor(out KernelProcessorState cpu))
            return KernelScheduler.Yield(cpu.Index,out UInt64 _) ? 0L : (Int64)KernelSystemCallError.Busy;
        Int64 custom=DispatchRegisteredAbi(KernelSystemCallAbi.Linux,frame);
        return custom;
    }

    private static Int64 DispatchNt(KernelSystemCallFrame* frame)
    {
        Int64 custom=DispatchRegisteredAbi(KernelSystemCallAbi.Nt,frame);
        return custom==(Int64)KernelSystemCallError.NotImplemented ? (Int64)(UInt64)KernelNtStatus.NotImplemented : custom;
    }

    private static Boolean Register(KernelSystemCallOperation operation, UInt32 service, delegate*<KernelSystemCallFrame*, Int64> handler)
    {
        if (handler==null || !KernelSystemCallMath.IsRegistrableService(service)) return false;
        fixed (UInt64* get=_registry.Get, set=_registry.Set, evt=_registry.Event)
        {
            UInt64 value=(UInt64)(void*)handler;
            if (operation==KernelSystemCallOperation.Get) get[service]=value;
            else if (operation==KernelSystemCallOperation.Set) set[service]=value;
            else if (operation==KernelSystemCallOperation.Event) evt[service]=value;
            else return false;
        }
        return true;
    }

    private static Boolean RegisterAbi(KernelSystemCallAbi abi, UInt32 service, delegate*<KernelSystemCallFrame*, Int64> handler)
    {
        if (handler==null || !KernelSystemCallMath.IsRegistrableService(service)) return false;
        fixed (UInt64* linux=_registry.Linux, nt=_registry.Nt)
        {
            UInt64 value=(UInt64)(void*)handler;
            if (abi==KernelSystemCallAbi.Linux) linux[service]=value;
            else if (abi==KernelSystemCallAbi.Nt) nt[service]=value;
            else return false;
        }
        return true;
    }

    private static Int64 DispatchRegistered(KernelSystemCallOperation operation, KernelSystemCallFrame* frame)
    {
        if (!KernelSystemCallMath.IsRegistrableService(frame->ServiceNumber)) return (Int64)KernelSystemCallError.NotImplemented;
        UInt64 address=0UL;
        fixed (UInt64* get=_registry.Get, set=_registry.Set, evt=_registry.Event)
        {
            if (operation==KernelSystemCallOperation.Get) address=get[frame->ServiceNumber];
            else if (operation==KernelSystemCallOperation.Set) address=set[frame->ServiceNumber];
            else if (operation==KernelSystemCallOperation.Event) address=evt[frame->ServiceNumber];
        }
        return Invoke(address,frame,(Int64)KernelSystemCallError.NotImplemented);
    }

    private static Int64 DispatchRegisteredAbi(KernelSystemCallAbi abi, KernelSystemCallFrame* frame)
    {
        if (!KernelSystemCallMath.IsRegistrableService(frame->ServiceNumber)) return (Int64)KernelSystemCallError.NotImplemented;
        UInt64 address=0UL;
        fixed (UInt64* linux=_registry.Linux, nt=_registry.Nt)
        {
            address=abi==KernelSystemCallAbi.Linux ? linux[frame->ServiceNumber] : nt[frame->ServiceNumber];
        }
        return Invoke(address,frame,(Int64)KernelSystemCallError.NotImplemented);
    }

    private static Int64 Invoke(UInt64 address, KernelSystemCallFrame* frame, Int64 missing)
    {
        if (address==0UL) return missing;
        delegate*<KernelSystemCallFrame*, Int64> handler=(delegate*<KernelSystemCallFrame*, Int64>)(void*)address;
        return handler(frame);
    }

    private static Boolean ValidateUserRange(UInt64 address, UInt64 byteCount, Boolean write)
    {
        if (!KernelProtectionMath.IsUserRange(address,byteCount)) return false;
        UInt64 current=address, remaining=byteCount;
        while (remaining!=0UL)
        {
            if (!KernelVirtualMemory.TryTranslate(current,out KernelVirtualTranslation translation)) return false;
            KernelVirtualMemoryProtection p=translation.Protection;
            if ((p & KernelVirtualMemoryProtection.User)==0 || (p & KernelVirtualMemoryProtection.Read)==0) return false;
            if (write && (p & KernelVirtualMemoryProtection.Write)==0) return false;
            UInt64 pageSize=(UInt64)translation.PageSize;
            UInt64 pageRemaining=pageSize-(current & (pageSize-1UL));
            UInt64 step=remaining<pageRemaining ? remaining : pageRemaining;
            current+=step; remaining-=step;
        }
        return true;
    }
}
