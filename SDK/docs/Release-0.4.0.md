# NovaOryn 0.4.0

Feature release implementing roadmap item 11: early allocator and kernel heap.

- Adds heap contracts and custom-methodology extension points.
- Adds caller-range bump early allocation and first-fit heap methodology assemblies.
- Adds a 64 KiB no-heap freestanding early arena.
- Adds a page-backed first-fit freestanding kernel heap inside the standard kernel heap reservation.
- Heap growth obtains physical frames through the PMM and maps them through the VMM as read/write, global, non-executable pages.
- Adds exact token/range release validation, free-block coalescing, zero filling and statistics.
- Generated kernels initialize and exercise the early allocator and heap before halting.
- Reduces the default framebuffer glyph height from 32 pixels to 16 pixels, making displayed text half the prior linear size while retaining the embedded NovaOryn Mono 8x16 glyph source.
