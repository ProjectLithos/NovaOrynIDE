using NovaOryn.Kernel.Smp;

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("[FAIL] " + name);
    Console.WriteLine("[ OK ] " + name);
}

Assert(KernelSmpMath.IsValidStartupTrampoline(0x8000), "A 4 KiB-aligned address below 1 MiB is a valid SIPI trampoline.");
Assert(!KernelSmpMath.IsValidStartupTrampoline(0x8001), "SIPI trampoline rejects unaligned addresses.");
Assert(!KernelSmpMath.IsValidStartupTrampoline(0x100000), "SIPI trampoline rejects addresses at or above 1 MiB.");
Assert(KernelSmpMath.TryGetStartupVector(0x9F000, out byte vector) && vector == 0x9F, "SIPI vector is derived from physical page number.");
Assert(KernelSmpMath.IsXApicDestination(255) && !KernelSmpMath.IsXApicDestination(256), "xAPIC destination IDs are limited to eight bits.");
Assert(KernelSmpMath.TryGetStateTableBytes(64, 48, out ulong bytes) && bytes == 3072, "Per-CPU table sizing is deterministic.");
Assert(!KernelSmpMath.TryGetStateTableBytes(0, 48, out _), "Per-CPU table sizing rejects zero processors.");
Console.WriteLine("[ OK ] SMP and per-CPU state methodology tests passed.");
