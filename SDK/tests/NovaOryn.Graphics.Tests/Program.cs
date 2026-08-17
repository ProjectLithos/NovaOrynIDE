using System;
using NovaOryn.Kernel.Graphics;
internal static class Program
{
    private static Int32 Main()
    {
        KernelGraphicsMode mode=new(1280U,720U,1280U,KernelGraphicsPixelFormat.BlueGreenRedReserved8);
        if(!mode.IsValid||mode.RequiredBytes!=3686400UL)return 1;
        KernelGraphicsFramebuffer framebuffer=new(0x100000UL,0xFFFF800000100000UL,mode.RequiredBytes,mode);
        if(!framebuffer.IsValid)return 2;
        KernelGraphicsMode invalid=new(1920U,1080U,1280U,KernelGraphicsPixelFormat.BlueGreenRedReserved8);
        if(invalid.IsValid)return 3;
        KernelGraphicsCapabilities compatibility=new(true,1U,1U,0U,new KernelGraphicsDisplayHandle(1U));
        if(compatibility.SimpleFramebuffers!=0U||compatibility.VirtioGpus!=0U)return 4;
        KernelGraphicsCapabilities complete=new(true,3U,1U,1U,1U,new KernelGraphicsDisplayHandle(2U));
        if(complete.Displays!=3U||complete.FirmwareFramebuffers!=1U||complete.SimpleFramebuffers!=1U||complete.VirtioGpus!=1U)return 5;
        if((Byte)KernelGraphicsTargetKind.SimpleFramebuffer!=3U)return 6;
        Console.WriteLine("NovaOryn graphics contract tests passed.");
        return 0;
    }
}
