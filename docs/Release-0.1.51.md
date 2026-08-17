# NovaOryn IDE 0.1.51

NovaOryn IDE 0.1.51 adds the Driver Development Centre and embeds NovaOryn SDK 0.38.0 with its first stable API/ABI contract manifest.

## Driver Development Centre

- PCI/PCIe, USB, VirtIO and platform driver project templates.
- Device-ID editor and explicit MMIO/PIO/interrupt/MSI/MSI-X/DMA/timer capability declarations.
- `NovaOryn.Driver.json` driver manifests tied to SDK API and driver ABI versions.
- Optional individual Test Explorer project per generated driver.
- Inventory of configured and developer-created driver projects.

## SDK contract

The bundled SDK now publishes `NovaOryn.SdkManifest.json`, stable public API/ABI version numbers, compatibility policy, and build validation of those contracts.
