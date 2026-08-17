# NovaOryn Architecture

NovaOryn separates managed kernel policy from architecture, boot, memory, console, runtime, compiler, linker, image, and launch concerns.

The current x64 UEFI path is:

```text
UEFI firmware
 -> native x64 EFI entry
 -> Graphics Output Protocol discovery
 -> final UEFI memory-map capture
 -> ExitBootServices
 -> NovaOryn-owned no-CoreLib NativeAOT bootstrap
 -> managed serial/framebuffer console
 -> managed KMain
 -> repeating CLI/HLT loop
```

The native entry performs only the firmware ABI work that must occur before managed execution. Framebuffer validation, clearing, pixel-format conversion, bitmap-font rendering, cursor movement, and serial/framebuffer mirroring are managed C# responsibilities.

The reusable SDK memory architecture is layered separately from the minimal bootstrap path:

```text
NovaOryn.Boot.Contracts
 -> NovaOryn.Memory.Contracts
 -> NovaOryn.Boot.Memory
 -> NovaOryn.Memory.Physical.Contracts
 -> NovaOryn.Memory.Physical
 -> kernel address-space design (next)
 -> virtual memory management
 -> early allocator / kernel heap
```

`NovaOryn.Boot.Memory` turns the final firmware map plus explicit NovaOryn reservations into a normalised ownership map. `NovaOryn.Memory.Physical` consumes only ranges marked immediately allocatable and provides bitmap, buddy, and extent frame allocators through `IPhysicalMemoryManager`. Their metadata storage is caller-owned so physical allocation does not depend on the future kernel heap.

The ordinary SDK surface exposes boot data through `NovaOryn.Boot.Contracts`, reusable memory ownership through `NovaOryn.Memory.Contracts`, physical allocation through the two `NovaOryn.Memory.Physical*` assemblies, and framebuffer output through `NovaOryn.Console.Framebuffer`. Architecture-specific CPU and port operations remain in `NovaOryn.Architecture.X64`.
