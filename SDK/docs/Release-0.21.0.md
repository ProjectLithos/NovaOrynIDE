# NovaOryn 0.21.0

NovaOryn 0.21.0 completes the opaque x64 driver interrupt broker requested by the APIC roadmap.

The new `NovaOryn.Kernel.InterruptBroker` assembly installs into `KernelDrivers` and owns route allocation from the driver-framework request through `KernelInterruptDispatch`. It exposes platform capabilities for Local APIC, I/O APIC, x2APIC, MSI, and MSI-X while keeping the selected mechanism hidden from ordinary drivers.

PCI delivery policy is MSI-X first, MSI second, and I/O APIC/INTx fallback. Generic MSI/MSI-X programming was moved into `NovaOryn.Kernel.Pci`; NVMe no longer exposes driver-specific MSI/MSI-X programming APIs. E1000/E1000e and RTL8168/RTL8111 request opaque interrupt handles and enable their device-local interrupt masks only when a broker route was allocated.

The release adds independent interrupt-broker policy tests, generated-template synchronization rules, build-policy checks preventing MSI/MSI-X programming from leaking back into NVMe, bootstrap capability diagnostics, and VSIX/template integration.

PCI INTx fallback currently consumes the firmware-programmed PCI Interrupt Line value and MADT I/O APIC ranges. Full ACPI `_PRT` namespace routing is intentionally identified as a later enhancement rather than fabricated in this release.
