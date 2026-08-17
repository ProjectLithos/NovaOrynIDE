using NovaOryn.Kernel.Protection;
static void Assert(bool c,string n){if(!c)throw new InvalidOperationException("[FAIL] "+n);Console.WriteLine("[ OK ] "+n);}
Assert(KernelProtectionMath.IsUserAddress(0x10000UL),"User null-guard boundary is accepted.");
Assert(!KernelProtectionMath.IsUserAddress(0xFFFF800000000000UL),"Kernel-half address is rejected as user memory.");
Assert(KernelProtectionMath.IsUserRange(0x400000UL,4096UL),"Normal user page range is accepted.");
Assert(!KernelProtectionMath.IsUserRange(0x7FFFFFFFF000UL,8192UL),"User range cannot cross the canonical lower-half boundary.");
Assert(KernelProtectionMath.IsValidUserStack(0x00007FFFFFFFE000UL),"16-byte-aligned user stack is valid.");
Assert(!KernelProtectionMath.IsValidUserStack(0x00007FFFFFFFE008UL),"Misaligned user stack is rejected.");
Assert(KernelProtectionMath.UserDataSelector==0x1B && KernelProtectionMath.UserCodeSelector==0x23,"Ring-3 selectors match the installed GDT ABI.");
Console.WriteLine("[ OK ] User/kernel separation methodology tests passed.");
