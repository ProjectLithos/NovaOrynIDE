# NovaOryn 0.26.1

NovaOryn 0.26.1 is a framebuffer performance corrective release for 0.26.0.

## Framebuffer presentation

- Keeps single, double, and triple buffering.
- Keeps font preset 3 and triple buffering as the defaults.
- Tracks framebuffer dirty rectangles.
- A normal glyph presentation copies only the modified glyph rectangle to GOP.
- Triple buffering copies the same dirty rectangle into the alternate software backbuffer before rotating draw buffers.
- Full-frame copies remain only for buffer initialization/synchronization or operations that genuinely redraw the whole screen.

This removes the 0.26.0 behavior where every character caused one or two full-frame memory copies, which made QEMU console output progressively slow during verbose bootstrap logging.
