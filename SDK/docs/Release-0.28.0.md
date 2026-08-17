# NovaOryn 0.28.0

NovaOryn 0.28.0 establishes the display-driver subsystem.

## Generic framebuffer

`NovaOryn.Kernel.Graphics` provides a heap-backed display registry, generic framebuffer and mode contracts, presentation callbacks, primary-display selection, and driver-owned mode changes. The existing UEFI GOP framebuffer is registered as the initial firmware framebuffer target and remains available as the reliable boot and real-hardware fallback. The same contract can represent VESA-like/simple linear framebuffers where a boot environment supplies one.

## VirtIO GPU

`NovaOryn.Kernel.Virtio.Gpu` is NovaOryn's first proper graphics driver. It binds VirtIO PCI GPU device type 16 independently of the existing VirtIO block/network/console/RNG family, negotiates the modern VirtIO transport, configures a control virtqueue, discovers enabled scan-outs, creates 2D resources, attaches DMA-backed framebuffer storage, selects scan-out resources, transfers modified rectangles to the host, flushes scan-out updates, and can recreate resources for runtime resolution changes.

The QEMU launcher now exposes a `virtio-gpu-pci` device so this path can be exercised without requiring physical GPU hardware.

## Scope

Native AMD, NVIDIA, and Intel GPU drivers are deliberately deferred. Early real-machine graphics remains based on EFI GOP or other simple linear framebuffers, while VirtIO GPU is the reference driver for NovaOryn's graphics-driver framework.
