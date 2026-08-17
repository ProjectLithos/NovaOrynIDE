# NovaOryn 0.0.89

NovaOryn 0.0.89 corrects the managed console contract and completes the separation of raw serial I/O from the end-user-facing console API.

`KernelConsole.Write(String)` and `KernelConsole.WriteLine(String)` are now ordinary safe managed C# methods. `Write(String)` uses the terminating freestanding `System.String` indexer supplied by `NovaOryn.Freestanding.CoreLib`; it no longer exposes an `unsafe` method signature or pins the string inside the console API.

COM1 initialization, COM1 port addresses, and the raw `NovaOrynX64WritePort8` import now reside exclusively in `NovaOryn.Kernel.X64.LowLevel.dll`. The raw `WritePort8` P/Invoke is private. `NovaOryn.Kernel.Console.dll` calls only the higher-level `Native.InitializeSerial()` and `Native.WriteSerial(Byte)` operations and contains no port numbers or raw port-I/O method names.

The authoritative bootstrap, command-line kernel template, and Visual Studio kernel template contain byte-identical `KernelConsole.cs` and `Native.cs` implementations. Source-policy tests now require the safe public `Write` and `WriteLine` signatures and reject raw serial I/O in the console assembly.

This release does not change the end-user `Kernel.cs`, GDT/TSS installation, IDT installation, interrupt-controller initialization, NativeAOT entry bridge, linker inputs, disk-image construction, or QEMU launch configuration.
