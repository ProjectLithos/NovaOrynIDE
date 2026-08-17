# NovaOryn 0.27.0

NovaOryn 0.27.0 introduces the modular USB subsystem.

## Assemblies

- `NovaOryn.Bus.Usb` — USB descriptors, device/interface/endpoint models, host-controller callbacks, enumeration, and transfer routing.
- `NovaOryn.Usb.Xhci` — PCI xHCI discovery, BAR mapping, controller reset/start, and root-port inspection.
- `NovaOryn.Usb.Hid` — USB HID boot-protocol keyboard and mouse discovery/report decoding.
- `NovaOryn.Usb.MassStorage` — Bulk-Only Transport, SCSI READ CAPACITY/READ(10)/WRITE(10)/SYNCHRONIZE CACHE, and removable block-device registration.
- `NovaOryn.Usb.Hub` — hub descriptor discovery, port power/status/reset, and downstream enumeration coordination.

## Integration

The bootstrap and generated Visual Studio kernel template initialize xHCI after PCI discovery and initialize USB hub/HID/mass-storage class layers after the storage core. USB mass storage uses the existing `KernelStorage` block-device registry and is identified as `KernelStorageDeviceKind.UsbMassStorage`.

EHCI/OHCI/UHCI are intentionally not expanded in this release; xHCI is the primary USB host-controller target.
