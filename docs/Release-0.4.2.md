# NovaOryn IDE 0.4.2

NovaOryn IDE 0.4.2 turns the previously defined SDK subsystem contracts and driver capability declarations into live kernel policy.

## Kernel runtime integration

- Kernel boot now validates all 13 formal subsystem boundaries at contract version 1.0 before the interactive runtime is enabled.
- Memory, interrupts, scheduler, processes, syscalls, drivers, filesystem, networking, graphics, input, time, power and SMP are checked against their concrete initialized implementations.
- Boot reports the ready/degraded subsystem count and fails cleanly if a required boundary is unavailable or incompatible.

## Capability-based drivers

- PCI device binding now causes the kernel to issue explicit capability grants from the driver's declaration and the device's discovered resources.
- A declaration remains only a privilege ceiling; it is not itself authority.
- Live grant lookup and validation are available to driver code.
- PCI devices expose DMA authority and MSI/MSI-X-capable devices expose interrupt authority even when no legacy interrupt line exists.
- VirtIO, VirtIO GPU, Intel E1000/E1000e and Realtek RTL8168/RTL8111 now register non-zero capability declarations.

This release is intended to make the kernel consume the professional SDK boundaries instead of merely shipping them as metadata/documentation.
