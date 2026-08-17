# NovaOryn display drivers

NovaOryn separates the generic graphics target from the mechanism that supplied it. The graphics core is `NovaOryn.Kernel.Graphics`.

## Firmware and simple framebuffers

`FirmwareFramebuffer` adapts the UEFI GOP framebuffer captured during boot. It remains the boot-safe and real-hardware fallback and does not require a native GPU driver.

`SimpleFramebuffer` explicitly registers a CPU-visible linear framebuffer supplied by another firmware interface, bootloader, VESA-like environment, or platform. It accepts physical and CPU-visible virtual addresses, byte length, dimensions, scan-line pitch, and the generic NovaOryn pixel format. Simple framebuffers do not claim hardware mode-setting capability; they expose the mode handed to NovaOryn.

Both providers register through `KernelGraphics.RegisterDisplay`, so consumers can enumerate them, select a primary target, inspect their current framebuffer, and present dirty rectangles through the same API used by proper graphics drivers.

## VirtIO GPU

`NovaOryn.Kernel.Virtio.Gpu` remains NovaOryn's first proper graphics driver. It owns VirtIO PCI GPU device type 16, discovers displays, creates 2D resources and backing storage, selects scan-outs, transfers framebuffer changes to the host, flushes them, and can recreate resources for runtime resolution changes. QEMU exposes `virtio-gpu-pci` for this path.

## Native GPUs

AMD, NVIDIA, and Intel native GPU drivers are intentionally deferred. Early physical-machine support should continue to use UEFI GOP or a simple linear framebuffer where available, while VirtIO GPU exercises the graphics-driver framework and mode/resource lifecycle in QEMU.
