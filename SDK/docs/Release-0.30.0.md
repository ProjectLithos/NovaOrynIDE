# NovaOryn 0.30.0

NovaOryn 0.30.0 introduces proper framebuffer font-face support for the QEMU/UEFI GOP console.

- Adds `ConsoleFontFormat` and `ConsoleFontInformation`.
- Adds validated PSF2 font installation from kernel-accessible memory.
- Parses PSF2 Unicode tables when present for ASCII console-character lookup.
- Separates font face from rendered font size.
- Keeps presets 1/2/3 as 8/16/24 px, with preset 3 as the default.
- Reflows and redraws retained console history when the face changes.
- Keeps the embedded NovaOryn Mono face as the guaranteed boot/recovery fallback.
- Mirrors the console-font implementation into both generated-kernel template trees.

No TrueType/OpenType rasterizer is introduced in this release; those scalable outline formats require a separate rasterization/shaping subsystem rather than being incorrectly treated as console bitmaps.
