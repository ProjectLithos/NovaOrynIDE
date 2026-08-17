using NovaOryn.Kernel.Pci;

static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}

PciLocation location=new(0x1234,0x56,0x1F,7);
Assert(location.Encode()==0x123456FFU,"PCI location packs all segment/bus/device/function bits.");
PciLocation decoded=PciLocation.Decode(location.Encode());
Assert(decoded.Segment==0x1234&&decoded.Bus==0x56&&decoded.Device==0x1F&&decoded.Function==7,"PCI location round-trip succeeds.");
Assert(PciMath.IsValidLocation(decoded),"Maximum legal PCI device/function address is accepted.");
Assert(PciMath.ShouldUseLegacyConfiguration(new PciLocation(0,0,0,0),0xFC),"Segment-zero conventional configuration uses CF8/CFC without ECAM remapping.");
Assert(!PciMath.ShouldUseLegacyConfiguration(new PciLocation(0,0,0,0),0x100),"PCIe extended configuration remains on ECAM.");
Assert(!PciMath.ShouldUseLegacyConfiguration(new PciLocation(1,0,0,0),0x00),"Non-zero PCI segments remain on ECAM.");
Assert(PciMath.BarLength32(0xFFFFF000U,false)==0x1000UL,"32-bit memory BAR sizing mask decodes to 4 KiB.");
Assert(PciMath.BarLength32(0xFFFFFF01U,true)==0x100UL,"I/O BAR sizing mask ignores flag bits.");
Assert(PciMath.BarLength64(0xFFFFFFFFFFFF0000UL)==0x10000UL,"64-bit BAR sizing mask decodes to 64 KiB.");
Assert(PciMath.IsConventionalCapabilityOffset(0x40)&&!PciMath.IsConventionalCapabilityOffset(0x41),"Conventional capability offsets require dword alignment.");
Assert(PciMath.IsExtendedCapabilityOffset(0x100)&&!PciMath.IsExtendedCapabilityOffset(0x0FC),"PCIe extended capabilities begin at 0x100.");
Console.WriteLine("[ OK ] PCI/PCIe tests passed.");
