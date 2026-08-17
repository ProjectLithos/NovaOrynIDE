# Nova Oryn OS SDK 0.0.28

## Managed framebuffer build correction

Release 0.0.28 corrects the build failure in `NovaOryn.Console.Framebuffer` reported by C# compiler error CS0118.

The assembly namespace is `NovaOryn.Console.Framebuffer`, while the boot contract also defines a type named `Framebuffer`. Inside that namespace, the unqualified type name was resolved as the namespace rather than `NovaOryn.Boot.Contracts.Framebuffer`.

`FramebufferConsole.cs` now declares the explicit alias:

```csharp
using BootFramebuffer = NovaOryn.Boot.Contracts.Framebuffer;
```

The backing field and `BootContext.TryGetFramebuffer` local now use `BootFramebuffer`. No public API, framebuffer layout, serial mirroring, rendered text, or halt behaviour is changed.

## Acceptance

Run:

```text
Build-NovaOryn.bat
```

The solution must compile `NovaOryn.Console.Framebuffer`, continue through ILC and EFI linking, boot QEMU, display the managed framebuffer output, capture the same serial output, and leave QEMU open in `CPU.Halt()`.
