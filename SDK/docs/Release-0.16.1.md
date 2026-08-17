# NovaOryn 0.16.1

Corrective patch for the NVMe/AHCI 0.16.0 integration.

- Fixes NVMe synthetic namespace-device registration to pass UInt16 vendor/device identifiers to `KernelDeviceIdentifier`.
- Fixes AHCI synthetic SATA-disk registration to pass UInt16 vendor/device identifiers to `KernelDeviceIdentifier`.
- Fixes `NovaOryn.TemplatePolicy.Tests` scope so NVMe, AHCI, and VirtIO generated-project reference checks execute inside the intended loop.
- Keeps canonical SDK, command-line template, and Visual Studio template driver sources synchronized.
