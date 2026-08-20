# NovaOryn 0.10.11

NovaOryn 0.10.11 adds automatic scrolling to the framebuffer console used for kernel output in the QEMU display.

## Framebuffer scrolling

When output advances beyond the final drawable text line, the console now:

1. moves framebuffer rows upward by one configured rendered line height;
2. preserves the configured top margin;
3. clears the newly exposed bottom strip using the configured background colour; and
4. continues output on the final visible text line instead of returning failure.

The scroll amount is derived from the active font renderer's line height, so font-size and spacing changes remain compatible.

## SDK and template parity

The behaviour is implemented in both `NovaOryn.Console.Framebuffer` and the freestanding `NovaOryn.Kernel.Console` framebuffer implementation. The command-line and Visual Studio kernel template copies of the freestanding console are kept byte-for-byte identical to the authoritative SDK source.

`NovaOryn.TemplatePolicy.Tests` now verifies both template parity and the presence of automatic scrolling so generated kernels cannot silently regress to a non-scrolling console.
