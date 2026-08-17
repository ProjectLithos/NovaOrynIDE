# NovaOryn 0.12.0

NovaOryn 0.12.0 implements roadmap item 19: driver framework.

## Added

- `NovaOryn.Kernel.Drivers`, a freestanding driver/device framework.
- Allocation-free driver callbacks for probe, start, stop, remove, and interrupt dispatch.
- Bus/vendor/device/class matching with class masks.
- Fixed-capacity device resource tables for MMIO, I/O ports, interrupts, DMA, and bus-specific resources.
- Exclusive device-to-driver binding and explicit lifecycle state.
- A transport-neutral interrupt request broker; ordinary drivers do not select PIC, I/O APIC, MSI, or MSI-X.
- `NovaOryn.Drivers.Tests` as an independent executable test program.
- Driver-framework initialization and visible startup status in the generated command-line and Visual Studio kernel templates.

## Capacity

The bootstrap implementation supports 64 registered drivers, 128 registered devices, and eight resources per device without managed allocation.

## Roadmap

Item 20 is storage and filesystems, which can now be layered on block-device drivers registered through this framework.
