# NovaOryn 0.18.4

NovaOryn 0.18.4 fixes direct NativeAOT/ILC code generation for ordinary large unmanaged value-type copies used by the ACPI FADT platform driver.

## Freestanding CoreLib

- Adds the `System.SpanHelpers` type expected by the .NET 10 NativeAOT JIT helper resolver.
- Adds `ClearWithoutReferences(ref Byte, UIntPtr)` using a direct freestanding byte-clear loop.
- Adds overlap-safe `Memmove(ref Byte, ref Byte, UIntPtr)` using direct forward/backward byte-copy loops.
- Uses no GC, Windows runtime, standard runtime library, allocation, or external native dependency.
- Keeps the helper methods internal so they do not expand the public SDK API or violate the no-public-void policy.

## Why this is required

`AcpiFadtInfo` is a normal unmanaged value type large enough for RyuJIT to lower assignment to a bulk memory operation. .NET 10 ILC resolves that operation through `System.SpanHelpers`. The previous freestanding CoreLib did not contain that normal runtime helper owner, causing ILC to stop while compiling `KernelAcpiFadt.Initialize()`.

This patch fixes the CoreLib capability rather than restructuring ACPI to avoid normal C# value-type semantics.

## Policy

`NovaOryn.BuildPolicy.Tests` now verifies that the freestanding CoreLib retains both required SpanHelpers entry points.
