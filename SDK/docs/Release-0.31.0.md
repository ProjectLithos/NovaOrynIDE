# NovaOryn 0.31.0

NovaOryn 0.31.0 adds a reproducible Linux-kernel console-font pack for the framebuffer/QEMU console.

The pack is pinned to upstream Linux commit `a13307e97d5c54b65720bb71fa379960ded1e51a` and contains five GPL-2.0 bitmap faces when installed:

- VGA 8x8
- VGA 8x16
- VGA 6x11
- Sun 8x16
- Sun 12x22

`Install-NovaOrynFonts.bat` downloads the exact upstream `lib/fonts/*.c` tables, verifies the GPL-2.0 SPDX header, extracts all 256 glyphs, and writes PSF2 files to `.toolchain/Fonts/LinuxKernel`. The original downloaded C tables are retained beside the generated PSF2 files for provenance.

`Install-NovaOrynToolchain.ps1` invokes the font installer after the normal toolchain has been validated. Font assets are therefore kept out of the source tree while still being installed as part of the NovaOryn SDK/toolchain.
