# NovaOryn 0.25.1

Corrective build-policy release for 0.25.0.

## Fixed

- Removed obsolete policy assertions that required `NovaOryn.Kernel.Console` to decode raw PS/2 scan codes directly.
- Build policy now verifies that `NovaOryn.Kernel.Ps2` decodes Up/Down and number keys 1-3 into `Ps2Key` events.
- Build policy now verifies that the bootstrap input consumer maps decoded Up/Down events to scrollback and decoded 1/2/3 events to framebuffer font presets.
- Added a regression assertion that `KernelConsole` does not read i8042 ports after `KernelPs2` owns the controller.

The 0.25.0 CoreLib/NovaOryn.String changes and the PS/2 runtime implementation are unchanged.
