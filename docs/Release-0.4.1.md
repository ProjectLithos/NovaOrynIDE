# NovaOryn IDE 0.4.1

NovaOryn IDE 0.4.1 advances the bundled SDK with a capability-based driver security model.

Drivers now register an explicit maximum privilege declaration and the kernel issues per-binding, revocable capability grants. The model covers MMIO ranges, port-I/O ranges, IRQ, MSI, MSI-X, DMA, PCI configuration, physical memory, timers, networking and filesystem access. Range capabilities are constrained to device resources, and privileged consumers can validate the opaque kernel-issued grant token.

The Driver Development Centre exposes all eleven capability classes, generated drivers register their declaration with `KernelDrivers`, `NovaOryn.Driver.json` advances to schema 2, and the OS-specific analyzer checks the four newly added service/resource capability classes in addition to the existing checks.
