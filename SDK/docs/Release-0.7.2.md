# NovaOryn 0.7.2

NovaOryn 0.7.2 is a corrective release for roadmap item 14, SMP and per-CPU state.

The 0.7.0 solution build exposed one freestanding C# type-conversion error in `NovaOryn.Kernel.Smp/KernelSmpMath.cs`. `TryGetStartupVector(UInt64, out Byte)` initialized its `Byte` output parameter with the `UInt32` literal `0U`, which the compiler correctly rejected as an implicit `UInt32`-to-`Byte` conversion.

This release changes that initialization to an explicit byte conversion in the authoritative SDK source and in both the command-line and Visual Studio project-template copies. The startup-vector calculation, SIPI encoding, AP trampoline, INIT/SIPI sequencing, per-CPU state model, bootstrap stacks, and public SMP APIs are otherwise unchanged.

## Patch correction
- Generated Boot/HAL startup diagnostics now use the structured kernel logging path.
- Existing OS refresh treats Boot/HAL as SDK-owned generated sources while preserving user Kernel\Kernel.cs.
- IDE serial output buffers incomplete kernel lines so polling cannot split a single kernel record into multiple `[KERNEL]` fragments.
