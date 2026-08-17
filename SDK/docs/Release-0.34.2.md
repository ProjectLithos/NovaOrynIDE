# NovaOryn 0.34.2

NovaOryn 0.34.2 fixes interactive QEMU keyboard input and Linux-kernel Sun font conversion.

- PS/2 keyboard and mouse can now be delivered by their real I/O APIC hardware IRQs (IRQ/GSI 1 and 12) through the interrupt broker instead of depending solely on timer polling.
- The i8042 IRQ enable bits are turned on only after interrupt routes are installed; timer servicing remains as a safe drain fallback.
- The Linux-kernel font converter now locates the actual `font_data` bitmap initializer rather than relying on VGA-specific glyph comments, so Sun 8x16 and Sun 12x22 convert to PSF2 as well.
- Existing PS/2 and USB HID command-line event handlers remain unchanged.
