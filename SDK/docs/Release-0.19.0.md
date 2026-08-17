# NovaOryn 0.19.0

NovaOryn 0.19.0 adds interactive framebuffer scrollback and runtime font-size control to the QEMU kernel console.

## Added

- Retained 256 KiB framebuffer-console text history.
- Up/Down arrow-key scrollback navigation.
- Font presets on number keys 1, 2, and 3: 8 px, 16 px, and 24 px.
- Full history reflow/redraw when the font size changes.
- PS/2 scan-code set 1 and set 2 make-code decoding for the supported keys.
- A high-level `KernelConsole.RunInteractive()` post-boot loop.
- An x64 `NovaOrynX64Pause` primitive used by the interactive idle loop.
- ProjectCreator migration from the old generated `CPU halted.` tail to the interactive console tail.
- QEMU runtime acceptance based on interactive-console readiness rather than permanent CPU halt output.

## Compatibility

`KernelPlatform.Halt()` remains available for kernels that intentionally need a permanent non-interactive halt. The generated kernel template now chooses the interactive console loop by default so the framebuffer remains controllable after startup.
