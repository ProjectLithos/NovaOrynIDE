# NovaOryn IDE 0.8.3

## Kernel console input reliability

The interactive boot/debug console now owns a minimal PS/2 keyboard path that is independent of the configured OS Input work area.

This fixes microkernel configurations where the Input subsystem is correctly assigned outside the kernel but the kernel boot console is still enabled.

- `KernelCommandLine.Initialize()` initializes `KernelPs2` when the HAL did not.
- Decoded keyboard events are routed directly to `KernelCommandLine`.
- `KernelConsole.RunInteractive()` invokes a non-blocking PS/2 service before each wait.
- Timer-driven PS/2 service remains installed when a keyboard is present.
- Hardware IRQ delivery remains supported when the configured HAL enables it, but is no longer required for console input.
- The general OS Input subsystem remains configuration controlled; this change only guarantees the kernel boot/debug shell remains usable.
