# NovaOryn 0.27.2

NovaOryn 0.27.2 is the second USB build-correction release for the modular USB subsystem introduced in 0.27.0.

## Fixes

- `NovaOryn.Usb.Xhci.KernelXhci` now uses an integral shift count accepted by C# when constructing downstream USB route strings. This removes CS0019 without changing the route-string value or topology model.
- `NovaOryn.Usb.MassStorage.UsbMassStorage` now allocates each 10-byte SCSI command descriptor block once before the per-block read/write loop and reuses it for each iteration. This removes CA2014 while preserving the existing one-block BOT transaction behavior.
- `NovaOryn.Usb.Hub.UsbHub` now allocates the temporary 12-byte hub descriptor buffer once before interface discovery and reuses it while probing hubs. This removes CA2014.

All three corrected implementation files are mirrored into both SDK kernel template trees.
