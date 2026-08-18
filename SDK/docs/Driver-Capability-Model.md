# Capability-based driver model

NovaOryn drivers declare the maximum privileges they need. Declaration is not authorization: the kernel grants each privilege explicitly for a bound device and returns an opaque `KernelDriverCapabilityGrant`.

## Capabilities

- MMIO ranges
- port I/O ranges
- IRQ
- MSI
- MSI-X
- DMA
- PCI configuration
- physical-memory ranges
- timers
- networking
- filesystem access

`KernelDriverCapabilityDeclaration` is registered with the driver. `KernelDriverCapabilityRequest` asks for one concrete privilege. MMIO, port-I/O and physical-memory requests must name a bounded range. The request is rejected if the privilege was not declared or if the range is outside the resources assigned to the device. PCI configuration grants are restricted to PCI devices. IRQ/MSI/MSI-X and DMA grants require corresponding device resources.

Kernel-issued grants are binding-specific and revocable. `ValidateCapabilityGrant` is the common authorization check for privileged subsystem entry points. Removing the device destroys all grants with the device record.

The generated `NovaOryn.Driver.json` schema is version 2 and records the driver's declared maximum privilege set. The IDE static analyzer checks obvious privileged API use against that declaration before boot.
