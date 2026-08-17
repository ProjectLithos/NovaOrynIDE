using NovaOryn.Kernel.Serial;
static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}
Assert(SerialMath.IsPciSerialController(0x070000U),"PCI communications/serial class is recognized.");
Assert(!SerialMath.IsPciSerialController(0x020000U),"Non-serial PCI classes are rejected.");
Assert(SerialMath.Is16550CompatibleProgrammingInterface(0x070002U),"PCI 16550 programming interface is supported.");
Assert(SerialMath.Is16550CompatibleProgrammingInterface(0x070006U),"PCI 16950-compatible programming interface remains register compatible.");
Assert(!SerialMath.Is16550CompatibleProgrammingInterface(0x070080U),"Vendor-specific serial programming interfaces are not guessed.");
Assert(SerialMath.TryCalculateDivisor(115200U,out ushort divisor)&&divisor==1,"115200 baud selects divisor one.");
Assert(SerialMath.TryCalculateDivisor(9600U,out divisor)&&divisor==12,"9600 baud selects divisor twelve.");
Assert(!SerialMath.TryCalculateDivisor(12345U,out _),"Unsupported fractional UART divisors are rejected.");
Console.WriteLine("[ OK ] Serial tests passed.");
