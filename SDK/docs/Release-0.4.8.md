# NovaOryn 0.4.9

NovaOryn 0.4.9 replaces the monolithic source-policy test executable with independent policy test programs.

The build now runs API, build/toolchain, boot, memory, template/VSIX, documentation, and release/updater policy executables separately. A failure identifies the responsible program directly and no test shares a 1,500-line top-level local-variable scope.

No PMM, VMM, address-space, heap, framebuffer, or runtime algorithm is changed by this corrective release.
