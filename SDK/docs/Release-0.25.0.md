# NovaOryn 0.25.0

0.25.0 begins the modular freestanding managed-library architecture.

- keeps compiler/runtime-required `System.*` members in `NovaOryn.Freestanding.CoreLib`
- adds `Object.ToString()`, `String.ToString()`, and allocation-free `Boolean.ToString()`
- adds the separate `NovaOryn.String` assembly with allocation-free Boolean and signed/unsigned integer formatting helpers
- adds `KernelConsole.Write/WriteLine` Boolean overloads, including prefix + Boolean forms
- adds `NovaOryn.String` to generated CLI and Visual Studio kernel templates
- documents why arbitrary runtime `String.Concat` is not faked before a correct managed-string allocator exists

For the reported ACPI EC status output, use:

```csharp
Boolean ecReady = KernelAcpiEc.Initialize();
KernelConsole.WriteLine("ecReady Status = ", ecReady);
```
