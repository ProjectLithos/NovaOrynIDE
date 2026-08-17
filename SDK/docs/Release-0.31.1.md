# NovaOryn 0.31.1

NovaOryn 0.31.1 is a focused console-font compilation fix based on 0.31.0.

## Fixed

- Corrected the embedded-font fallback in `ConsoleFont.GetGlyphRow` so it passes the existing `Byte` glyph value directly to `BitmapFont.GetGlyphRow`.
- Mirrored the correction into both generated-kernel SDK/template trees.
- Preserved PSF2 font installation, Linux-kernel font-pack support, font-size presets, and embedded fallback behaviour without API changes.

## Build impact

The normal fast kernel build can now compile `NovaOryn.Kernel.Console` without the CS1503 `char`-to-`byte` mismatch.
