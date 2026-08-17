# NovaOryn 0.0.62

This release corrects the freestanding-kernel integration path.

- Normal builds now compile `src/NovaOryn.Kernel.Bootstrap`, the authoritative in-repository ILC input.
- The booting kernel installs a bootstrap-processor GDT and 64-bit TSS with RSP0 and dedicated double-fault, NMI, and machine-check IST stacks.
- The booting kernel creates and loads all 256 x64 IDT gates.
- Both legacy PICs are masked before APIC/MSI-era interrupt delivery is used.
- The framebuffer and serial consoles visibly report each completed initialization stage.
- `InterruptControllers.obj` is now included in the native link rather than only being existence-checked.
- Source-policy tests inspect the actual ILC bootstrap and prevent regression to sample-only integration.

Explicit `-Project` builds remain supported for user kernels.
