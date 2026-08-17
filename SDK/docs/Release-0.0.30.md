# Nova Oryn OS SDK 0.0.30

Release 0.0.30 fixes the external C# kernel compilation contract introduced in 0.0.29.

## Correction

`NovaOryn.ManagedCompiler` previously derived the managed DLL name from the project filename. For `NovaOrynKernel.csproj`, that caused it to look for `NovaOrynKernel.dll`, even though the project explicitly declares:

```xml
<AssemblyName>NovaOryn.Kernel.Bootstrap</AssemblyName>
```

The compiler now reads the evaluated project declaration directly and therefore locates:

```text
Artifacts\MinimalKernel\ManagedIL\NovaOryn.Kernel.Bootstrap.dll
```

The same resolved assembly name is passed to ILC through `--systemmodule`. If a kernel project omits `<AssemblyName>`, the compiler safely falls back to the `.csproj` filename.

No framebuffer, serial, UEFI, image, QEMU, or `CPU.Halt()` behaviour is changed.
