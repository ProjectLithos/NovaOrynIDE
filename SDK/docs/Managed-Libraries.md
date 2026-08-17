# NovaOryn managed library architecture

NovaOryn keeps `NovaOryn.Freestanding.CoreLib` deliberately small. CoreLib owns only the `System.*` types and members required by C#, Roslyn, and NativeAOT to compile and execute freestanding code.

Higher-level functionality belongs in selectable assemblies. NovaOryn 0.25.0 begins that split with `NovaOryn.String`, which provides allocation-free value formatting. Planned peers include `NovaOryn.Math`, `NovaOryn.Hashing`, `NovaOryn.Cryptography`, `NovaOryn.Encryption`, `NovaOryn.Collections`, `NovaOryn.IO`, and `NovaOryn.Threading`; these should be implemented as real facilities rather than empty placeholder assemblies.

## Boolean output

The no-GC bootstrap does not yet provide a general managed-string allocator, so arbitrary runtime `String.Concat` is intentionally not faked. Use the value-aware console overload:

```csharp
Boolean ecReady = KernelAcpiEc.Initialize();
KernelConsole.WriteLine("ecReady Status = ", ecReady);
```

This writes the same visible result without allocating a temporary managed string. `Boolean.ToString()` is also supplied by CoreLib and returns the normal `True` or `False` literal without allocation.

General runtime `string + value` support should be enabled only when NovaOryn has a correct managed-string allocation path; it must not be implemented with mutable string literals, hidden global scratch strings, or other non-.NET semantics.
