# NovaOryn 0.33.0

NovaOryn 0.33.0 completes the initial display-driver stage on top of 0.32.0.

- Keeps the UEFI GOP framebuffer as the boot-safe `FirmwareFramebuffer` generic graphics target.
- Adds `SimpleFramebuffer` as an explicit adapter for VESA-like, bootloader-provided, or other platform linear framebuffers.
- Tracks simple framebuffer targets separately in `KernelGraphicsCapabilities` while retaining the existing compatibility constructor.
- Keeps all firmware/simple/driver-owned displays behind the same heap-backed `KernelGraphics` registry, primary-display selection, framebuffer metadata, and dirty-region presentation API.
- Retains `NovaOryn.Kernel.Virtio.Gpu` as NovaOryn's first proper graphics driver with VirtIO PCI discovery, display-info discovery, 2D resources, backing memory, scan-out selection, transfer/flush presentation, and runtime mode/resource recreation.
- Continues to expose `virtio-gpu-pci` in QEMU.
- Deliberately does not add AMD, NVIDIA, or Intel native GPU drivers at this stage.
