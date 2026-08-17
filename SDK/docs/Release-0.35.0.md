# NovaOryn 0.35.0

## Interactive console

- Adds a blinking vertical caret at the active command insertion position.
- Adds a persistent vertical scrollbar representing retained framebuffer history and scrollback offset.
- Reserves scrollbar width from text layout so glyphs cannot overwrite it.
- Updates caret/scrollbar through dirty framebuffer regions while retaining automatic double buffering.

## Visual Studio kernel structure

New generated kernels now contain:

- `Kernel/Kernel.cs` — high-level orchestration only.
- `Boot/BootStartup.cs` — boot/runtime initialization.
- `HAL/HardwareAbstractionLayer.cs` — hardware detection, configuration, drivers and device servicing.

## Userland structure

Adds the `src/Userland` aggregate and sub-projects `Commands`, `Settings`, `Fonts`, `Images` and `Drivers`. Existing font/buffering/keyboard userland projects remain compatibility facades backed by the canonical Commands project.
