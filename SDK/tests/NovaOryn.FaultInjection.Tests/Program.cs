using System;
using NovaOryn.Kernel.Contracts;

internal static unsafe class Program
{
    private static readonly (KernelFaultKind Kind,String Subsystem)[] Cases = new[]
    {
        (KernelFaultKind.AllocationFailure,"heap"),(KernelFaultKind.IoTimeout,"storage"),(KernelFaultKind.DroppedInterrupt,"interrupt"),
        (KernelFaultKind.DeviceReset,"device"),(KernelFaultKind.BadDma,"dma"),(KernelFaultKind.CorruptPacket,"network"),
        (KernelFaultKind.PageFault,"virtual-memory"),(KernelFaultKind.CpuOffline,"smp"),(KernelFaultKind.FilesystemError,"fatfs")
    };

    private static Int32 Main()
    {
        UInt32 passed=0;
        foreach(var item in Cases)
        {
            KernelFaultInjection.Reset();
            if(!KernelFaultInjection.TryArm(item.Kind,item.Subsystem,1,1,0x10,out UInt64 id)||id==0) return Fail(item.Kind,"arm");
            if(KernelFaultInjection.ShouldInject(item.Kind,item.Subsystem,out _)) return Fail(item.Kind,"triggered too early");
            if(!KernelFaultInjection.ShouldInject(item.Kind,item.Subsystem,out UInt64 parameter)||parameter!=0x10) return Fail(item.Kind,"did not trigger");
            if(KernelFaultInjection.ShouldInject(item.Kind,item.Subsystem,out _)) return Fail(item.Kind,"repeat limit ignored");
            if(!KernelFaultInjection.TryDisarm(id)) return Fail(item.Kind,"disarm");
            Console.WriteLine($"[ OK ] {item.Kind} deterministic trigger/repeat/disarm"); passed++;
        }
        KernelFaultInjection.Reset();
        if(!KernelFaultInjection.TryArm(KernelFaultKind.BadDma,"dma",0,1,0x1000,out _)) return 1;
        if(!KernelFaultInjection.TryCorruptDmaAddress("dma",0x2000,out UInt64 dma)||dma!=0x3000) return Fail(KernelFaultKind.BadDma,"DMA corruption helper");
        Byte* packet=stackalloc Byte[4];packet[0]=1;packet[1]=2;packet[2]=3;packet[3]=4;
        KernelFaultInjection.Reset();KernelFaultInjection.TryArm(KernelFaultKind.CorruptPacket,"network",0,1,2,out _);
        if(!KernelFaultInjection.TryCorruptPacket("network",packet,4,out UInt32 offset,out Byte original)||offset!=2||original!=3||packet[2]!=(Byte)(3^0xFF)) return Fail(KernelFaultKind.CorruptPacket,"packet corruption helper");
        Console.WriteLine($"[ OK ] NovaOryn fault injection suite: {passed} fault kinds plus corruption helpers."); return 0;
    }
    private static Int32 Fail(KernelFaultKind kind,String stage){Console.WriteLine($"[FAIL] {kind}: {stage}");return 1;}
}
