using System;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Time;
using NovaOryn.Kernel.Internal.X64;

namespace NovaOryn.Kernel.Scheduler;

/// <summary>Owns kernel-thread records, per-CPU run state, priority queues, affinity and preemption decisions.</summary>
public static unsafe class KernelScheduler
{
    private const UInt32 MaximumThreads = 256U;
    private const UInt64 DefaultStackBytes = 65536UL;
    private const UInt64 DefaultQuantumNanoseconds = 5000000UL;
    private const UInt32 NoProcessor = 0xFFFFFFFFU;
    private const UInt64 ContextBytes = 256UL;

    private struct ThreadRecord
    {
        internal UInt64 Id, StackBase, StackTop, EntryPoint, Argument, AffinityMask, ContextAddress;
        internal UInt32 State, Priority, ProcessorIndex, NextReady;
    }
    private struct CpuScheduleState
    {
        internal UInt32 CurrentSlot, IdleSlot;
        internal UInt64 SwitchCount, PreemptionCount, LastDispatchNanoseconds;
    }

    private static ThreadRecord* _threads;
    private static CpuScheduleState* _cpus;
    private static UInt32 _processorCount, _activeThreads;
    private static UInt64 _nextThreadId = 1UL, _quantum = DefaultQuantumNanoseconds;
    private static Boolean _initialized, _timerPreemption;

    /// <summary>Initializes scheduler tables after SMP, heap and clock initialization.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelSmp.IsInitialized() || !KernelTime.IsInitialized) return false;
        _processorCount = KernelSmp.GetProcessorCount();
        if (_processorCount == 0U) return false;
        if (!KernelSchedulerMath.TryGetTableBytes(MaximumThreads, (UInt32)sizeof(ThreadRecord), out UInt64 threadBytes)) return false;
        if (!KernelSchedulerMath.TryGetTableBytes(_processorCount, (UInt32)sizeof(CpuScheduleState), out UInt64 cpuBytes)) return false;
        if (!KernelHeap.TryAllocate(threadBytes, 64UL, true, out KernelHeapAllocation threadAlloc)) return false;
        if (!KernelHeap.TryAllocate(cpuBytes, 64UL, true, out KernelHeapAllocation cpuAlloc)) return false;
        _threads = (ThreadRecord*)threadAlloc.Address; _cpus = (CpuScheduleState*)cpuAlloc.Address;
        for (UInt32 cpu=0U; cpu<_processorCount; cpu++)
        {
            CpuScheduleState* state=_cpus+cpu; state->CurrentSlot=0xFFFFFFFFU; state->IdleSlot=0xFFFFFFFFU;
            if (!KernelSmp.TrySetSchedulerContext(cpu, (UInt64)state)) return false;
        }
        if (!CreateBootstrapThread()) return false;
        _timerPreemption = KernelTime.GetCapabilities().HasLocalApicTimer;
        _initialized=true; return true;
    }

    /// <summary>Gets whether scheduler state has been initialized.</summary>
    public static Boolean IsInitialized() => _initialized;
    /// <summary>Gets scheduler capabilities and current usage.</summary>
    public static KernelSchedulerCapabilities GetCapabilities() => new KernelSchedulerCapabilities(_processorCount, MaximumThreads, _activeThreads, _quantum, _timerPreemption);
    /// <summary>Gets the number of active thread records.</summary>
    public static UInt32 GetActiveThreadCount() => _activeThreads;
    /// <summary>Gets the configured scheduler quantum.</summary>
    public static UInt64 GetQuantumNanoseconds() => _quantum;
    /// <summary>Sets the scheduler quantum and re-arms timer preemption when available.</summary>
    public static Boolean SetQuantumNanoseconds(UInt64 nanoseconds)
    {
        if (!_initialized) return false; _quantum=KernelSchedulerMath.ClampQuantum(nanoseconds); return true;
    }

    /// <summary>Creates a ready kernel thread with a page-aligned kernel stack and initial x64 entry context.</summary>
    public static Boolean TryCreateThread(UInt64 entryPoint, UInt64 argument, KernelThreadPriority priority, UInt64 affinityMask, UInt64 stackBytes, out UInt64 threadId)
    {
        threadId=0UL; if (!_initialized || entryPoint==0UL || affinityMask==0UL) return false;
        if (stackBytes==0UL) stackBytes=DefaultStackBytes;
        if (!KernelSchedulerMath.IsValidStackSize(stackBytes)) return false;
        if (!FindUnusedSlot(out UInt32 slot)) return false;
        if (!KernelHeap.TryAllocate(stackBytes, 4096UL, true, out KernelHeapAllocation stack)) return false;
        if (!KernelHeap.TryAllocate(ContextBytes, 16UL, true, out KernelHeapAllocation context)) { KernelHeap.TryRelease(stack); return false; }
        if (!Native.InitializeThreadContext(context.Address, stack.Address+stack.ByteCount, entryPoint, argument)) { KernelHeap.TryRelease(context); KernelHeap.TryRelease(stack); return false; }
        ThreadRecord* r=_threads+slot; r->Id=_nextThreadId++; r->StackBase=stack.Address; r->StackTop=stack.Address+stack.ByteCount; r->EntryPoint=entryPoint; r->Argument=argument; r->AffinityMask=affinityMask; r->ContextAddress=context.Address; r->State=(UInt32)KernelThreadState.Ready; r->Priority=(UInt32)priority; r->ProcessorIndex=NoProcessor; r->NextReady=0xFFFFFFFFU;
        _activeThreads++; threadId=r->Id; return true;
    }

    /// <summary>Gets a stable snapshot of a thread.</summary>
    public static Boolean TryGetThread(UInt64 threadId, out KernelThreadInfo info)
    {
        info=default; if (!FindThread(threadId,out UInt32 slot)) return false; ThreadRecord* r=_threads+slot;
        info=new KernelThreadInfo(r->Id,(KernelThreadState)r->State,(KernelThreadPriority)r->Priority,r->ProcessorIndex,r->AffinityMask,r->StackBase,r->StackTop,r->EntryPoint,r->Argument); return true;
    }
    /// <summary>Changes a thread affinity mask while preserving at least one allowed processor.</summary>
    public static Boolean SetAffinity(UInt64 threadId, UInt64 affinityMask)
    { if (affinityMask==0UL || !FindThread(threadId,out UInt32 slot)) return false; (_threads+slot)->AffinityMask=affinityMask; return true; }
    /// <summary>Blocks a non-terminated thread until Wake is requested.</summary>
    public static Boolean Block(UInt64 threadId)
    { if (!FindThread(threadId,out UInt32 slot)) return false; ThreadRecord* r=_threads+slot; if (r->State==(UInt32)KernelThreadState.Terminated) return false; r->State=(UInt32)KernelThreadState.Blocked; r->ProcessorIndex=NoProcessor; return true; }
    /// <summary>Makes a blocked thread runnable again.</summary>
    public static Boolean Wake(UInt64 threadId)
    { if (!FindThread(threadId,out UInt32 slot)) return false; ThreadRecord* r=_threads+slot; if (r->State!=(UInt32)KernelThreadState.Blocked) return false; r->State=(UInt32)KernelThreadState.Ready; return true; }
    /// <summary>Marks a thread terminated so it can no longer be selected.</summary>
    public static Boolean Terminate(UInt64 threadId)
    { if (!FindThread(threadId,out UInt32 slot)) return false; ThreadRecord* r=_threads+slot; r->State=(UInt32)KernelThreadState.Terminated; r->ProcessorIndex=NoProcessor; return true; }

    /// <summary>Selects the highest-priority runnable thread allowed on a processor.</summary>
    public static Boolean TrySelectNext(UInt32 processorIndex, out UInt64 threadId)
    {
        threadId=0UL; if (!_initialized || processorIndex>=_processorCount) return false; UInt32 best=0xFFFFFFFFU; UInt32 bestPriority=0U;
        for (UInt32 slot=0U;slot<MaximumThreads;slot++) { ThreadRecord* r=_threads+slot; if (r->State!=(UInt32)KernelThreadState.Ready || !KernelSchedulerMath.AllowsProcessor(r->AffinityMask,processorIndex)) continue; if (best==0xFFFFFFFFU || r->Priority>bestPriority) { best=slot; bestPriority=r->Priority; } }
        if (best==0xFFFFFFFFU) return false; threadId=(_threads+best)->Id; return true;
    }

    /// <summary>Performs the scheduler decision associated with a cooperative yield.</summary>
    public static Boolean Yield(UInt32 processorIndex, out UInt64 nextThreadId) => Schedule(processorIndex,false,out nextThreadId);
    /// <summary>Performs the scheduler decision associated with a Local APIC timer tick.</summary>
    public static Boolean OnTimerTick(UInt32 processorIndex, out UInt64 nextThreadId) => Schedule(processorIndex,true,out nextThreadId);

    private static Boolean Schedule(UInt32 cpu, Boolean preempt, out UInt64 nextThreadId)
    {
        nextThreadId=0UL; if (!_initialized || cpu>=_processorCount) return false; CpuScheduleState* cs=_cpus+cpu;
        if (cs->CurrentSlot!=0xFFFFFFFFU) { ThreadRecord* current=_threads+cs->CurrentSlot; if (current->State==(UInt32)KernelThreadState.Running) { current->State=(UInt32)KernelThreadState.Ready; current->ProcessorIndex=NoProcessor; } }
        if (!TrySelectNext(cpu,out nextThreadId)) return false; if (!FindThread(nextThreadId,out UInt32 slot)) return false; ThreadRecord* next=_threads+slot; UInt32 previousSlot=cs->CurrentSlot; ThreadRecord* previous=previousSlot==0xFFFFFFFFU ? null : _threads+previousSlot; next->State=(UInt32)KernelThreadState.Running; next->ProcessorIndex=cpu; cs->CurrentSlot=slot; cs->SwitchCount++; if(preempt) cs->PreemptionCount++; cs->LastDispatchNanoseconds=KernelTime.GetMonotonicNanoseconds();
        if (previous!=null && previous!=next && previous->ContextAddress!=0UL && next->ContextAddress!=0UL) return Native.SwitchThreadContext(previous->ContextAddress,next->ContextAddress);
        return true;
    }
    private static Boolean CreateBootstrapThread()
    {
        if (!KernelSmp.TryGetCurrentProcessor(out KernelProcessorState cpu)) return false; if (!KernelHeap.TryAllocate(ContextBytes,16UL,true,out KernelHeapAllocation context)) return false; ThreadRecord* r=_threads; r->Id=_nextThreadId++; r->State=(UInt32)KernelThreadState.Running; r->Priority=(UInt32)KernelThreadPriority.Critical; r->ProcessorIndex=cpu.Index; r->AffinityMask=cpu.Index<64U ? 1UL<<(Int32)cpu.Index : 0xFFFFFFFFFFFFFFFFUL; r->ContextAddress=context.Address; _cpus[cpu.Index].CurrentSlot=0U; _activeThreads=1U; return true;
    }
    private static Boolean FindUnusedSlot(out UInt32 slot)
    { slot=0U; for(UInt32 i=1U;i<MaximumThreads;i++) if((_threads+i)->State==(UInt32)KernelThreadState.Unused){slot=i;return true;} return false; }
    private static Boolean FindThread(UInt64 id,out UInt32 slot)
    { slot=0U; if(id==0UL||_threads==null)return false; for(UInt32 i=0U;i<MaximumThreads;i++) if((_threads+i)->Id==id && (_threads+i)->State!=(UInt32)KernelThreadState.Unused){slot=i;return true;} return false; }
}
