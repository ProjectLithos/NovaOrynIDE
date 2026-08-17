using System;
using NovaOryn.Kernel.Rtl8168;

static void Assert(Boolean condition,String message){if(!condition)throw new Exception(message);Console.WriteLine("[ OK ] "+message);}
Assert(Rtl8168Math.Identify(0x10EC,0x8168)==Rtl8168ControllerFamily.Rtl8168Or8111,"Realtek RTL8168/RTL8111 PCI ID is identified.");
Assert(Rtl8168Math.Identify(0x10EC,0x8169)==Rtl8168ControllerFamily.Rtl8169,"Realtek RTL8169-compatible PCI ID is identified.");
Assert(Rtl8168Math.Identify(0x8086,0x100E)==Rtl8168ControllerFamily.Unknown,"Non-Realtek NICs are rejected by the RTL8168 matcher.");
Assert(Rtl8168Math.IsValidDescriptorCount(64),"RTL8168 DMA ring accepts a power-of-two 64 descriptor layout.");
Assert(Rtl8168Math.DescriptorBytes(64)==1024,"RTL8168 descriptor-ring byte sizing is deterministic.");
Console.WriteLine("[ OK ] Realtek RTL8168/RTL8111 tests passed.");
