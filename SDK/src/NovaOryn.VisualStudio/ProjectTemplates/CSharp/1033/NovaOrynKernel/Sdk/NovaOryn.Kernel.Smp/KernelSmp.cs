using System;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Smp;

/// <summary>Discovers x64 processors, owns per-CPU bootstrap state, and starts xAPIC application processors.</summary>
public static unsafe class KernelSmp
{
    private const UInt32 MaximumProcessors = 256U;
    private const UInt64 ApplicationProcessorStackBytes = 16384UL;
    private const UInt64 InitDelayNanoseconds = 10000000UL;
    private const UInt64 StartupDelayNanoseconds = 200000UL;
    private const UInt64 StartupTimeoutNanoseconds = 100000000UL;
    private const UInt32 LocalApicIcrLow = 0x300U;
    private const UInt32 LocalApicIcrHigh = 0x310U;
    private const UInt32 DeliveryPending = 0x1000U;
    private const UInt32 InitAssert = 0x0000C500U;
    private const UInt32 InitDeassert = 0x00008500U;
    private const UInt32 StartupIpi = 0x00000600U;
    private const UInt32 ApicBaseMsr = 0x1BU;
    private const UInt64 ApicEnabled = 1UL << 11;
    private const UInt64 X2ApicEnabled = 1UL << 10;
    private const UInt32 PerCpuStorageSlots = 8U;

    private struct PerCpuRecord
    {
        internal UInt32 Index;
        internal UInt32 ApicId;
        internal UInt32 AcpiUid;
        internal UInt32 Flags;
        internal UInt32 StartupState;
        internal UInt32 Reserved;
        internal UInt64 KernelStackBase;
        internal UInt64 KernelStackTop;
        internal UInt64 SchedulerContext;
        internal fixed UInt64 Storage[8];
    }

    private static PerCpuRecord* _records;
    private static UInt32 _processorCount;
    private static UInt32 _onlineCount;
    private static UInt32 _bootstrapIndex;
    private static UInt64 _trampolineAddress;
    private static UInt64 _localApicBase;
    private static Boolean _xApicStartup;
    private static Boolean _initialized;
    private static Byte _shutdownIpiVector;
    private static Byte _rescheduleIpiVector;
    private static Byte _tlbShootdownIpiVector;
    private static Byte _callFunctionIpiVector;
    private static KernelSmpStatus _status = KernelSmpStatus.NotInitialized;

    /// <summary>Initializes per-CPU records and starts each xAPIC application processor through INIT/SIPI.</summary>
    /// <returns><see langword="true"/> when per-CPU state is usable; individual AP failures are reported through status and processor snapshots.</returns>
    public static Boolean Initialize(BootContext boot)
    {
        if (_initialized) return true;
        if (!KernelAcpi.IsInitialized()) return false;
        _processorCount = KernelAcpi.GetProcessorCount();
        if (_processorCount == 0U) { _status = KernelSmpStatus.NoProcessors; return false; }
        if (_processorCount > MaximumProcessors) _processorCount = MaximumProcessors;
        if (!KernelSmpMath.TryGetStateTableBytes(_processorCount, (UInt32)sizeof(PerCpuRecord), out UInt64 tableBytes)) { _status = KernelSmpStatus.StateAllocationFailed; return false; }
        if (!KernelHeap.TryAllocate(tableBytes, 64UL, true, out KernelHeapAllocation stateAllocation)) { _status = KernelSmpStatus.StateAllocationFailed; return false; }
        _records = (PerCpuRecord*)stateAllocation.Address;
        if (!PopulateProcessorRecords()) { _status = KernelSmpStatus.NoProcessors; return false; }
        UInt32 currentApicId = Native.GetCurrentApicId();
        if (!FindProcessor(currentApicId, out _bootstrapIndex)) { _status = KernelSmpStatus.BootstrapProcessorNotFound; return false; }
        PerCpuRecord* bsp = _records + _bootstrapIndex;
        bsp->Flags |= 2U; bsp->StartupState = (UInt32)KernelProcessorStartupState.BootstrapProcessor;
        _onlineCount = 1U;
        _trampolineAddress = boot.GetApplicationProcessorTrampolineAddress();
        UInt64 apicBaseMsr = Native.ReadModelSpecificRegister(ApicBaseMsr);
        if ((apicBaseMsr & ApicEnabled) != 0UL && (apicBaseMsr & X2ApicEnabled) == 0UL)
        {
            _localApicBase = apicBaseMsr & 0x0000000FFFFFF000UL;
            if (_localApicBase == 0UL) KernelAcpi.TryGetLocalApicAddress(out _localApicBase);
            _xApicStartup = _localApicBase != 0UL;
        }
        _initialized = true;
        if (_processorCount == 1U) { _status = KernelSmpStatus.Success; return true; }
        if (!KernelSmpMath.IsValidStartupTrampoline(_trampolineAddress)) { MarkRemainingUnsupported(); _status = KernelSmpStatus.TrampolineUnavailable; return true; }
        if (!_xApicStartup) { MarkRemainingUnsupported(); _status = KernelSmpStatus.LocalApicUnavailable; return true; }
        StartApplicationProcessors();
        _status = _onlineCount == _processorCount ? KernelSmpStatus.Success : KernelSmpStatus.Partial;
        return true;
    }

    /// <summary>Gets whether the per-CPU state table completed initialization.</summary>
    public static Boolean IsInitialized() => _initialized;
    /// <summary>Gets the latest SMP initialization status.</summary>
    public static KernelSmpStatus GetLastStatus() => _status;
    /// <summary>Gets a freestanding-safe SMP status name.</summary>
    public static String GetLastStatusName()
    {
        if (_status == KernelSmpStatus.Success) return "Success";
        if (_status == KernelSmpStatus.Partial) return "Partial";
        if (_status == KernelSmpStatus.NoProcessors) return "NoProcessors";
        if (_status == KernelSmpStatus.StateAllocationFailed) return "StateAllocationFailed";
        if (_status == KernelSmpStatus.BootstrapProcessorNotFound) return "BootstrapProcessorNotFound";
        if (_status == KernelSmpStatus.TrampolineUnavailable) return "TrampolineUnavailable";
        if (_status == KernelSmpStatus.LocalApicUnavailable) return "LocalApicUnavailable";
        return "NotInitialized";
    }
    /// <summary>Gets the number of enabled processors represented by per-CPU state.</summary>
    public static UInt32 GetProcessorCount() => _processorCount;
    /// <summary>Gets the number of processors that have completed the NovaOryn bootstrap handshake.</summary>
    public static UInt32 GetOnlineProcessorCount() => _onlineCount;
    /// <summary>Gets the logical index of the firmware bootstrap processor.</summary>
    public static UInt32 GetBootstrapProcessorIndex() => _bootstrapIndex;
    /// <summary>Gets an immutable capability snapshot.</summary>
    public static KernelSmpCapabilities GetCapabilities() => new(_processorCount, _onlineCount, _bootstrapIndex, _trampolineAddress, _xApicStartup, _xApicStartup, _xApicStartup && KernelSmpMath.IsValidStartupTrampoline(_trampolineAddress), _shutdownIpiVector >= 32U, PerCpuStorageSlots);

    /// <summary>Gets one per-CPU state snapshot by NovaOryn logical processor index.</summary>
    public static Boolean TryGetProcessor(UInt32 index, out KernelProcessorState processor)
    {
        processor = default;
        if (!_initialized || _records == null || index >= _processorCount) return false;
        PerCpuRecord* record = _records + index;
        processor = new KernelProcessorState(record->Index, record->ApicId, record->AcpiUid, (record->Flags & 1U) != 0U, (record->Flags & 2U) != 0U, (KernelProcessorStartupState)record->StartupState, record->KernelStackBase, record->KernelStackTop, record->SchedulerContext);
        return true;
    }

    /// <summary>Resolves the processor executing the current code to its per-CPU state snapshot.</summary>
    public static Boolean TryGetCurrentProcessor(out KernelProcessorState processor)
    {
        processor = default;
        if (!_initialized || !FindProcessor(Native.GetCurrentApicId(), out UInt32 index)) return false;
        return TryGetProcessor(index, out processor);
    }


    /// <summary>Enumerates one logical CPU through the stable formal SMP surface.</summary>
    public static Boolean TryEnumerateProcessor(UInt32 index, out KernelCpuInfo cpu)
    { cpu=default; if(!TryGetProcessor(index,out KernelProcessorState state))return false; Boolean online=state.StartupState==KernelProcessorStartupState.BootstrapProcessor||state.StartupState==KernelProcessorStartupState.OnlineParked; cpu=new KernelCpuInfo(state.Index,state.ApicId,state.AcpiUid,online,state.IsBootstrapProcessor,state.StartupState); return true; }

    /// <summary>Gets the zero-based logical CPU currently executing this code.</summary>
    public static Boolean TryGetCurrentProcessorIndex(out UInt32 processorIndex)
    { processorIndex=0U; if(!TryGetCurrentProcessor(out KernelProcessorState state))return false; processorIndex=state.Index; return true; }

    /// <summary>Reads one stable 64-bit per-CPU storage slot.</summary>
    public static Boolean TryGetPerCpuValue(UInt32 processorIndex, KernelPerCpuStorageKey key, out UInt64 value)
    { value=0UL; UInt32 slot=(UInt32)key; if(!_initialized||_records==null||processorIndex>=_processorCount||slot>=PerCpuStorageSlots)return false; value=(_records+processorIndex)->Storage[slot]; return true; }

    /// <summary>Writes one stable 64-bit per-CPU storage slot.</summary>
    public static Boolean TrySetPerCpuValue(UInt32 processorIndex, KernelPerCpuStorageKey key, UInt64 value)
    { UInt32 slot=(UInt32)key; if(!_initialized||_records==null||processorIndex>=_processorCount||slot>=PerCpuStorageSlots)return false; (_records+processorIndex)->Storage[slot]=value; if(key==KernelPerCpuStorageKey.Scheduler)(_records+processorIndex)->SchedulerContext=value; return true; }

    /// <summary>Configures a standard runtime IPI vector. Vectors below 32 are rejected.</summary>
    public static Boolean ConfigureIpiVector(KernelIpiPurpose purpose, Byte vector)
    { if(vector<32U)return false; if(purpose==KernelIpiPurpose.Reschedule)_rescheduleIpiVector=vector; else if(purpose==KernelIpiPurpose.TlbShootdown)_tlbShootdownIpiVector=vector; else if(purpose==KernelIpiPurpose.CallFunction)_callFunctionIpiVector=vector; else if(purpose==KernelIpiPurpose.CpuShutdown)_shutdownIpiVector=vector; else return false; return true; }

    /// <summary>Sends a fixed-delivery IPI to an online logical CPU.</summary>
    public static Boolean TrySendIpi(UInt32 processorIndex, Byte vector)
    { if(!_initialized||!_xApicStartup||vector<32U||processorIndex>=_processorCount)return false; PerCpuRecord* r=_records+processorIndex; if(r->StartupState!=(UInt32)KernelProcessorStartupState.BootstrapProcessor&&r->StartupState!=(UInt32)KernelProcessorStartupState.OnlineParked)return false; if(!KernelSmpMath.IsXApicDestination(r->ApicId))return false; return SendIpi(r->ApicId,(UInt32)vector); }

    /// <summary>Sends one configured standard IPI purpose to an online logical CPU.</summary>
    public static Boolean TrySendIpi(UInt32 processorIndex, KernelIpiPurpose purpose)
    { Byte vector=0; if(purpose==KernelIpiPurpose.Reschedule)vector=_rescheduleIpiVector; else if(purpose==KernelIpiPurpose.TlbShootdown)vector=_tlbShootdownIpiVector; else if(purpose==KernelIpiPurpose.CallFunction)vector=_callFunctionIpiVector; else if(purpose==KernelIpiPurpose.CpuShutdown)vector=_shutdownIpiVector; return vector>=32U&&TrySendIpi(processorIndex,vector); }

    /// <summary>Starts one offline application processor through the existing INIT/SIPI transport.</summary>
    public static Boolean TryStartProcessor(UInt32 processorIndex)
    { if(!_initialized||processorIndex>=_processorCount||processorIndex==_bootstrapIndex||!_xApicStartup||!KernelSmpMath.IsValidStartupTrampoline(_trampolineAddress))return false; PerCpuRecord* record=_records+processorIndex; if(record->StartupState==(UInt32)KernelProcessorStartupState.OnlineParked)return true; return StartApplicationProcessor(processorIndex); }

    /// <summary>Requests cooperative shutdown of an application processor using the configured CPU-shutdown IPI.</summary>
    public static Boolean TryShutdownProcessor(UInt32 processorIndex)
    { if(processorIndex>=_processorCount||processorIndex==_bootstrapIndex||_shutdownIpiVector<32U)return false; if(!TrySendIpi(processorIndex,KernelIpiPurpose.CpuShutdown))return false; (_records+processorIndex)->StartupState=(UInt32)KernelProcessorStartupState.ShutdownRequested; return true; }

    /// <summary>Completes cooperative shutdown bookkeeping on the calling CPU before it parks.</summary>
    public static Boolean NotifyCurrentProcessorOffline()
    { if(!TryGetCurrentProcessorIndex(out UInt32 index)||index==_bootstrapIndex)return false; PerCpuRecord* r=_records+index; if(r->StartupState==(UInt32)KernelProcessorStartupState.OnlineParked||r->StartupState==(UInt32)KernelProcessorStartupState.ShutdownRequested){r->StartupState=(UInt32)KernelProcessorStartupState.Offline;if(_onlineCount>0U)_onlineCount--;return true;}return false; }

    /// <summary>Gets the opaque CPU-local scheduler state token.</summary>
    public static Boolean TryGetSchedulerContext(UInt32 processorIndex,out UInt64 schedulerContext)
    { schedulerContext=0UL; if(!_initialized||_records==null||processorIndex>=_processorCount)return false; schedulerContext=(_records+processorIndex)->SchedulerContext; return schedulerContext!=0UL; }

    /// <summary>Stores the scheduler-owned context token reserved in one processor's per-CPU record.</summary>
    public static Boolean TrySetSchedulerContext(UInt32 index, UInt64 schedulerContext)
    {
        if (!_initialized || _records == null || index >= _processorCount) return false;
        (_records + index)->SchedulerContext = schedulerContext;
        (_records + index)->Storage[(UInt32)KernelPerCpuStorageKey.Scheduler] = schedulerContext;
        return true;
    }

    private static Boolean PopulateProcessorRecords()
    {
        if (_records == null) return false;
        for (UInt32 index = 0U; index < _processorCount; index++)
        {
            if (!KernelAcpi.TryGetProcessor(index, out AcpiProcessorInfo acpi)) return false;
            PerCpuRecord* record = _records + index;
            record->Index = index; record->ApicId = acpi.ApicId; record->AcpiUid = acpi.AcpiUid;
            record->Flags = acpi.IsX2Apic ? 1U : 0U; record->StartupState = (UInt32)KernelProcessorStartupState.Offline;
            record->Reserved = 0U; record->KernelStackBase = 0UL; record->KernelStackTop = 0UL; record->SchedulerContext = 0UL; for(UInt32 slot=0U;slot<PerCpuStorageSlots;slot++) record->Storage[slot]=0UL;
        }
        return true;
    }

    private static Boolean FindProcessor(UInt32 apicId, out UInt32 index)
    {
        index = 0U;
        if (_records == null) return false;
        for (UInt32 candidate = 0U; candidate < _processorCount; candidate++) if ((_records + candidate)->ApicId == apicId) { index = candidate; return true; }
        return false;
    }

    private static Boolean StartApplicationProcessors()
    {
        if (!KernelSmpMath.TryGetStartupVector(_trampolineAddress, out Byte vector)) return false;
        UInt64 pageTableRoot = Native.ReadPageTableRoot();
        if (pageTableRoot == 0UL || pageTableRoot > 0xFFFFFFFFUL) { MarkRemainingUnsupported(); return false; }
        for (UInt32 index = 0U; index < _processorCount; index++)
        {
            if (index == _bootstrapIndex) continue;
            StartApplicationProcessor(index);
        }
        return true;
    }

    private static Boolean StartApplicationProcessor(UInt32 index)
    {
        if(index>=_processorCount||index==_bootstrapIndex)return false;
        if(!KernelSmpMath.TryGetStartupVector(_trampolineAddress,out Byte vector))return false;
        UInt64 pageTableRoot=Native.ReadPageTableRoot(); if(pageTableRoot==0UL||pageTableRoot>0xFFFFFFFFUL)return false;
        PerCpuRecord* record=_records+index;
        if(KernelFaultInjection.ShouldInject(KernelFaultKind.CpuOffline,"smp",out _)){record->StartupState=(UInt32)KernelProcessorStartupState.Offline;return false;}
        if(!KernelSmpMath.IsXApicDestination(record->ApicId)){record->StartupState=(UInt32)KernelProcessorStartupState.Unsupported;return false;}
        if(record->KernelStackTop==0UL){if(!KernelHeap.TryAllocate(ApplicationProcessorStackBytes,16UL,true,out KernelHeapAllocation stackAllocation)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}record->KernelStackBase=stackAllocation.Address;record->KernelStackTop=stackAllocation.Address+ApplicationProcessorStackBytes;}
        record->StartupState=(UInt32)KernelProcessorStartupState.Starting;
        if(!Native.PrepareApplicationProcessorTrampoline(_trampolineAddress,pageTableRoot,record->KernelStackTop)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}
        if(!SendInitSequence(record->ApicId)||!SendIpi(record->ApicId,StartupIpi|vector)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}
        if(!KernelTime.DelayNanoseconds(StartupDelayNanoseconds)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}
        if(Native.GetApplicationProcessorStartupStatus(_trampolineAddress)==0U&&!SendIpi(record->ApicId,StartupIpi|vector)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}
        if(!WaitForStartup(record->ApicId)){record->StartupState=(UInt32)KernelProcessorStartupState.Failed;return false;}
        record->StartupState=(UInt32)KernelProcessorStartupState.OnlineParked;_onlineCount++;return true;
    }

    private static Boolean SendInitSequence(UInt32 apicId)
    {
        if (!SendIpi(apicId, InitAssert)) return false;
        if (!KernelTime.DelayNanoseconds(InitDelayNanoseconds)) return false;
        if (!SendIpi(apicId, InitDeassert)) return false;
        return KernelTime.DelayNanoseconds(StartupDelayNanoseconds);
    }

    private static Boolean SendIpi(UInt32 apicId, UInt32 command)
    {
        if (!WaitForIcrIdle()) return false;
        if (!Native.WriteMmio32(_localApicBase + LocalApicIcrHigh, apicId << 24)) return false;
        if (!Native.WriteMmio32(_localApicBase + LocalApicIcrLow, command)) return false;
        return WaitForIcrIdle();
    }

    private static Boolean WaitForIcrIdle()
    {
        if (!KernelTime.TryCreateDeadline(StartupTimeoutNanoseconds, out UInt64 deadline)) return false;
        while ((Native.ReadMmio32(_localApicBase + LocalApicIcrLow) & DeliveryPending) != 0U) if (KernelTime.HasReached(deadline)) return false;
        return true;
    }

    private static Boolean WaitForStartup(UInt32 expectedApicId)
    {
        if (!KernelTime.TryCreateDeadline(StartupTimeoutNanoseconds, out UInt64 deadline)) return false;
        while (Native.GetApplicationProcessorStartupStatus(_trampolineAddress) == 0U) if (KernelTime.HasReached(deadline)) return false;
        return Native.GetApplicationProcessorObservedApicId(_trampolineAddress) == expectedApicId;
    }

    private static Boolean MarkRemainingUnsupported()
    {
        if (_records == null) return false;
        for (UInt32 index = 0U; index < _processorCount; index++) if (index != _bootstrapIndex && (_records + index)->StartupState == (UInt32)KernelProcessorStartupState.Offline) (_records + index)->StartupState = (UInt32)KernelProcessorStartupState.Unsupported;
        return true;
    }
}
