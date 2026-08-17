# NovaOryn 0.28.2

NovaOryn 0.28.2 is a build-correction release for the VirtIO GPU display driver introduced in 0.28.0.

## Fixed

- Corrected the remaining `UInt32` to `UInt16` compile-time mismatch in the VirtIO GPU control-queue setup call.
- Kept the requested control queue size at 64 entries; only the literal's static type was corrected.
- Mirrored the corrected VirtIO GPU implementation into both NovaOryn kernel template trees.

The generic graphics subsystem, UEFI GOP framebuffer fallback, VirtIO GPU 2D resource model, display mode handling, and public graphics API are otherwise unchanged.
