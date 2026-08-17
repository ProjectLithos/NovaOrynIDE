# NovaOryn 0.34.3

NovaOryn 0.34.3 fixes the QEMU startup stall that occurred after xHCI discovery when the new PS/2 hardware-IRQ path attempted to route legacy keyboard/mouse interrupts.

- I/O APIC register pages are mapped as device MMIO through the kernel MMIO window rather than incorrectly translated through the PMM-managed RAM direct map.
- All ACPI-advertised I/O APICs are mapped and cached by GSI range before routes are installed.
- I/O APIC destination-high is programmed before the unmasked redirection-low entry to avoid a transient route with a stale destination.
- `KernelPs2.Initialize()` explicitly establishes `English_UK` because the freestanding ILC build uses `--nopreinitstatics` and cannot rely on non-zero static field initializers.
- Failure to install PS/2 hardware routes no longer aborts KMain; the existing timer service remains available as the fallback input path.
- Visual Studio/QEMU runtime acceptance now waits for the actual `NovaOryn> ` prompt instead of a preceding status message.
