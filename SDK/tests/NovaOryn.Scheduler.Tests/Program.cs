using NovaOryn.Kernel.Scheduler;
static void Assert(bool c,string n){if(!c)throw new InvalidOperationException("[FAIL] "+n);Console.WriteLine("[ OK ] "+n);}
Assert(KernelSchedulerMath.IsValidStackSize(16384UL),"Minimum 16 KiB stack is valid.");
Assert(KernelSchedulerMath.IsValidStackSize(65536UL),"Default 64 KiB stack is valid.");
Assert(!KernelSchedulerMath.IsValidStackSize(12288UL),"Stacks smaller than 16 KiB are rejected.");
Assert(!KernelSchedulerMath.IsValidStackSize(65537UL),"Stacks must remain page aligned.");
Assert(KernelSchedulerMath.AllowsProcessor(4UL,2U)&&!KernelSchedulerMath.AllowsProcessor(4UL,1U),"Affinity masks select logical processors deterministically.");
Assert(KernelSchedulerMath.ClampQuantum(1UL)==100000UL,"Quantum lower bound is enforced.");
Assert(KernelSchedulerMath.ClampQuantum(5000000UL)==5000000UL,"Normal quantum is preserved.");
Assert(KernelSchedulerMath.ClampQuantum(2000000000UL)==1000000000UL,"Quantum upper bound is enforced.");
Assert(KernelSchedulerMath.TryGetTableBytes(256U,64U,out ulong bytes)&&bytes==16384UL,"Scheduler table sizing is deterministic.");
Console.WriteLine("[ OK ] Scheduler and threads methodology tests passed.");
