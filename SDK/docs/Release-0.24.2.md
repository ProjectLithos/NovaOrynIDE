# NovaOryn 0.24.2

Corrective PS/2 input-delivery release.

## Fixed

- Corrected the generated-kernel input path that called `KernelPs2.Service()` and then attempted to reread the same i8042 bytes through `KernelConsole.ServiceInput()`.
- `KernelPs2` is now the sole owner of PS/2 controller reads.
- Added a decoded `Ps2KeyboardEvent` handler registration path.
- Generated kernels consume decoded events directly: Up/Down scroll, 1/2/3 change font size, and printable characters visibly echo on the live console.
- `KernelConsole.ServiceInput()` no longer accesses PS/2 hardware; it only reports whether the console is ready for driver-delivered input.
- Preserved full `English_UK` and `English_USA` translation tables and runtime layout switching.

## Runtime acceptance

With QEMU keyboard focus, ordinary printable keys must appear on the live framebuffer console. Up/Down must move through scrollback when scrollback exists, and 1/2/3 must visibly select 8/16/24-pixel font presets.
