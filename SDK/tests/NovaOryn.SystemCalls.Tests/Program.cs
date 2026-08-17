using NovaOryn.Kernel.SystemCalls;
static void Assert(bool c,string n){if(!c)throw new InvalidOperationException("[FAIL] "+n);Console.WriteLine("[ OK ] "+n);}
ulong get=KernelSystemCallMath.EncodeGetSetEvent(KernelSystemCallOperation.Get,7U);
Assert(KernelSystemCallMath.TryDecodeAbi(get,out var a)&&a==KernelSystemCallAbi.GetSetEvent,"Get/Set/Event namespace decodes independently.");
Assert(KernelSystemCallMath.GetOperation(get)==KernelSystemCallOperation.Get&&KernelSystemCallMath.GetServiceNumber(get)==7U,"Get operation and service ID round-trip.");
ulong set=KernelSystemCallMath.EncodeGetSetEvent(KernelSystemCallOperation.Set,9U);
Assert(KernelSystemCallMath.GetOperation(set)==KernelSystemCallOperation.Set,"Set operation remains distinct from Get.");
ulong evt=KernelSystemCallMath.EncodeGetSetEvent(KernelSystemCallOperation.Event,11U);
Assert(KernelSystemCallMath.GetOperation(evt)==KernelSystemCallOperation.Event,"Event operation remains distinct from Get and Set.");
ulong linux=KernelSystemCallMath.EncodeLinux(24U);
Assert(KernelSystemCallMath.TryDecodeAbi(linux,out a)&&a==KernelSystemCallAbi.Linux&&KernelSystemCallMath.GetServiceNumber(linux)==24U,"Linux-style numeric syscall IDs retain their number.");
ulong nt=KernelSystemCallMath.EncodeNt(0x37U);
Assert(KernelSystemCallMath.TryDecodeAbi(nt,out a)&&a==KernelSystemCallAbi.Nt&&KernelSystemCallMath.GetServiceNumber(nt)==0x37U,"NT-style service IDs use a separate namespace.");
Assert(!KernelSystemCallMath.TryDecodeAbi(24U,out _),"Unnamespaced raw values are rejected rather than ambiguously treated as Linux or NT.");
Assert(KernelSystemCallMath.IsRegistrableService(63U)&&!KernelSystemCallMath.IsRegistrableService(64U),"Custom handler registry remains bounded.");
Console.WriteLine("[ OK ] System-call methodology tests passed.");
