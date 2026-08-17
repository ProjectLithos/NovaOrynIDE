# NovaOryn 0.0.69

NovaOryn 0.0.69 removes all native-entry and low-level implementation detail from the end-user `Kernel.cs`.

The runtime export now lives in the separate `NovaOryn.Kernel.Entry.X64.dll`. Native imports remain in `NovaOryn.Kernel.X64.LowLevel.dll`; console hardware access remains hidden behind `NovaOryn.Kernel.Console.dll`; and descriptor, interrupt, PIC, and halt operations remain behind `NovaOryn.Kernel.Platform.X64.dll`.

`NovaOryn.ProjectCreator` now detects the known SDK-generated monolithic and 0.0.65–0.0.68 kernel sources, saves a `.pre-0.0.69.bak` copy, and installs the clean high-level kernel. User-authored kernels that do not match those generated forms remain untouched.
