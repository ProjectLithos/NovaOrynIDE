# NovaOryn IDE 0.7.7

Patch release completing the structured kernel logging presentation path.

- Routes structured kernel diagnostics to the serial/debug transport without painting diagnostic metadata into the interactive GOP framebuffer console.
- Keeps ordinary shell and user-facing console output mirrored to QEMU as before.
- Fixes the framebuffer registration diagnostic so `1280x800` is emitted as one structured record instead of starting a second record between width and height.
- Preserves allocation-free no-GC bootstrap logging with Trace, Debug, Info, Warning, Error and Critical levels plus subsystem, CPU, thread, process, timestamp and source context.
- Synchronizes the canonical console implementation with generated OS and Visual Studio templates.
- Extends logging verification to enforce serial-only diagnostic routing and prevent nested structured prefixes.
