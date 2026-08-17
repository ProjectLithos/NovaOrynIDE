# Nova Oryn OS SDK 0.20.1

NovaOryn 0.20.1 is a compile-correction release for the interrupt-dispatch split introduced in 0.20.0.

The 0.20.0 build reached `NovaOryn.Kernel.InterruptDispatch` and failed on the normal .NET declaration:

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 8)]
```

The interrupt declaration was correct. The incompatibility was in `NovaOryn.Freestanding.CoreLib`: its `System.Runtime.InteropServices.StructLayoutAttribute` only provided the constructor and omitted the standard named layout members.

0.20.1 extends the freestanding CoreLib implementation with the standard `Pack`, `Size`, and `CharSet` fields and the `Value` property. The constructor records its `LayoutKind` in `Value`. The interrupt frame therefore remains ordinary .NET-compatible C# rather than using a NovaOryn-specific workaround.

Build policy now enforces this CoreLib surface. No polling, timer-dispatch, interrupt-dispatch, console, network, storage, ACPI, or public driver behaviour is otherwise changed by this patch.
