# NovaOryn IDE 0.6.0

## Unified device model

NovaOryn 0.6.0 makes the kernel device tree the canonical model for hardware and software-visible devices.

- Six canonical classes: PCI, USB, ACPI, platform, virtual, and logical.
- Parent/child relationships are first-class and exposed through one `KernelDeviceNode` contract.
- USB interfaces are children of USB devices; downstream USB devices may be children of hubs.
- AHCI disks and NVMe namespaces are logical children of the PCI controller that discovered them.
- `KernelDrivers.GetDeviceTreeSnapshot`, `TryGetDeviceNodeByIndex`, and `TryGetRootDevice` provide tooling-friendly enumeration.
- Existing `Virtio` and `Synthetic` enum values remain compatibility aliases for virtual and logical devices.
- The IDE Hardware Tree no longer invents a separate browser-only model. It asks the project service for a `NovaOrynDeviceTreeSnapshot`, using the same six classes and hierarchy as the kernel contract.

The configured snapshot is available before boot; the contract is designed so runtime snapshots can replace it without changing the Hardware Tree UI.
