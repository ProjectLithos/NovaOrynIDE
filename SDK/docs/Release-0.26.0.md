# NovaOryn 0.26.0

NovaOryn 0.26.0 adds configurable single-, double-, and triple-buffered framebuffer-console output plus userland controls for both buffering and font size.

- Single buffering renders directly to the UEFI GOP/QEMU scan-out framebuffer.
- Double buffering renders to one kernel-heap software backbuffer and presents completed output to GOP.
- Triple buffering alternates between two kernel-heap software backbuffers while GOP remains the front/scan-out framebuffer.
- The kernel heap supplies both backbuffers and triple buffering is the default.
- Font preset 3 (24 px) is now the default.
- `font get`, `font set 1|2|3`, and `font list` use NovaOryn Get/Set service 33.
- `buffering get`, `buffering set 1|2|3`, and `buffering list` use NovaOryn Get/Set service 34.
- Preset 1/2/3 means 8/16/24 px for font and single/double/triple for buffering.
- Existing keyboard shortcuts remain available as boot-console conveniences, but userland no longer needs direct kernel-console access.
- CLI and Visual Studio generated kernel templates carry the same renderer, defaults, syscall registrations, and input mappings as the authoritative SDK.

UEFI GOP does not expose a portable hardware page-flip API, so NovaOryn implements double/triple buffering with heap-backed software framebuffer images and presents completed frames to the GOP scan-out framebuffer.
