using System;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Console;
using NovaOryn.Arch.X64;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Processes;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Bootstrap.Boot;

/// <summary>Freestanding, allocation-free bridge between KernelPanic and x64/platform services.</summary>
public static unsafe class KernelPanicTransport
{
    public static Boolean Initialize()
        => KernelPanic.ConfigureFreestanding(&GetContext,&CaptureRegisters,&CaptureCallStack,&RequestCrashDump,&BreakDebugger,&Halt,&Reboot);

    public static Boolean GetContext(UInt32* cpu,UInt64* threadId,UInt64* processId,UInt64* instructionPointer,UInt64* stackPointer,UInt64* timestampNanoseconds)
    {
        UInt32 c=0;UInt64 t=0,p=0,rip=0,rsp=0,rbp=0,flags=0,cr3=0;
        if(KernelSmp.TryGetCurrentProcessor(out KernelProcessorState processor))c=processor.Index;
        KernelScheduler.TryGetCurrentThreadId(out t);
        KernelProcesses.TryGetCurrentProcessId(out p);
        X64ArchitectureBoundary.CapturePanicContext(out rip,out rsp,out rbp,out flags,out cr3);
        if(cpu!=null)*cpu=c;if(threadId!=null)*threadId=t;if(processId!=null)*processId=p;
        if(instructionPointer!=null)*instructionPointer=rip;if(stackPointer!=null)*stackPointer=rsp;
        if(timestampNanoseconds!=null)*timestampNanoseconds=KernelTime.IsInitialized?KernelTime.GetMonotonicNanoseconds():0UL;
        return true;
    }

    public static Boolean CaptureRegisters(KernelPanicRegisters* snapshot)
    {
        if(snapshot==null)return false;
        UInt64 rip=0,rsp=0,rbp=0,flags=0,cr3=0;
        if(!X64ArchitectureBoundary.CapturePanicContext(out rip,out rsp,out rbp,out flags,out cr3))return false;
        // Volatile GPRs have already been used by the managed call boundary; leave them zero
        // rather than publishing misleading values. RIP/RSP/RBP/RFLAGS/CR3 are authoritative.
        *snapshot=new KernelPanicRegisters(0,0,0,0,0,0,rbp,rsp,0,0,0,0,0,0,0,0,rip,flags,cr3);
        return true;
    }

    public static Boolean CaptureCallStack(KernelPanicCallStack* stack)
    {
        if(stack==null)return false;
        UInt64 rip=0,rsp=0,rbp=0,flags=0,cr3=0;
        if(!X64ArchitectureBoundary.CapturePanicContext(out rip,out rsp,out rbp,out flags,out cr3))return false;
        UInt64 f0=rip,f1=0,f2=0,f3=0,f4=0,f5=0,f6=0,f7=0;UInt32 count=rip!=0?1U:0U;
        // Conservative frame-pointer walk. Stop immediately on non-monotonic or implausibly distant frames.
        UInt64 current=rbp;
        for(UInt32 index=1;index<8&&current!=0;index++)
        {
            UInt64* frame=(UInt64*)current;
            UInt64 next=frame[0],ret=frame[1];
            if(ret==0||next<=current||next-current>1048576UL)break;
            if(index==1)f1=ret;else if(index==2)f2=ret;else if(index==3)f3=ret;else if(index==4)f4=ret;else if(index==5)f5=ret;else if(index==6)f6=ret;else f7=ret;
            count=index+1;current=next;
        }
        *stack=new KernelPanicCallStack(count,f0,f1,f2,f3,f4,f5,f6,f7);
        return true;
    }

    public static Boolean RequestCrashDump(KernelPanicNativeInfo* info,KernelPanicRegisters* registers,KernelPanicCallStack* stack)
    {
        if(info==null)return false;
        // The IDE consumes this stable panic marker. If debugging is attached, the following
        // debugger break lets it materialise the full NOCD dump using registers/memory/page tables.
        if(!KernelConsole.Write("[NOVAORYN:PANIC] code="))return false;
        if(!KernelConsole.WriteUInt64(info->Code))return false;
        if(!KernelConsole.Write(" cpu="))return false;if(!KernelConsole.WriteUInt64(info->Cpu))return false;
        if(!KernelConsole.Write(" thread="))return false;if(!KernelConsole.WriteUInt64(info->ThreadId))return false;
        if(!KernelConsole.Write(" process="))return false;if(!KernelConsole.WriteUInt64(info->ProcessId))return false;
        if(!KernelConsole.WriteLine(" dump=1"))return false;
        return true;
    }

    public static Boolean BreakDebugger(KernelPanicNativeInfo* info)=>X64ArchitectureBoundary.PanicDebuggerBreak();
    public static Boolean Reboot()=>KernelAcpiPower.Reboot();
    public static Boolean Halt()=>X64ArchitectureBoundary.Halt();
}
