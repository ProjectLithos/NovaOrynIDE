using System;
using NovaOryn.Kernel.E1000;

static void Assert(Boolean condition,String message){if(!condition)throw new Exception(message);Console.WriteLine("[ OK ] "+message);}
Assert(E1000Math.Identify(0x8086,0x100E)==E1000ControllerFamily.E1000,"Intel 82540EM/QEMU E1000 PCI ID is identified.");
Assert(E1000Math.Identify(0x8086,0x10D3)==E1000ControllerFamily.E1000e,"Intel 82574L E1000e PCI ID is identified.");
Assert(E1000Math.Identify(0x8086,0x15F3)==E1000ControllerFamily.Unknown,"Intel I225-family IDs remain reserved for the later dedicated driver.");
Assert(E1000Math.Identify(0x10EC,0x8168)==E1000ControllerFamily.Unknown,"Non-Intel NICs are rejected by the E1000 matcher.");
Assert(E1000Math.IsValidDescriptorCount(64),"E1000 DMA ring accepts a power-of-two 64 descriptor layout.");
Assert(E1000Math.DescriptorBytes(64)==1024,"E1000 descriptor-ring byte sizing is deterministic.");
Console.WriteLine("[ OK ] Intel E1000/E1000e tests passed.");
