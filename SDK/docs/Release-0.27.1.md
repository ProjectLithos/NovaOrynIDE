# NovaOryn 0.27.1

NovaOryn 0.27.1 is a USB build-correction release for the modular USB subsystem introduced in 0.27.0.

## Fix

`NovaOryn.Bus.Usb.KernelUsbBus` no longer takes the address of method parameters or `out` parameters when forwarding control, bulk, and interrupt transfers through unsafe host-controller function pointers.

Each public transfer method now uses an addressable local value for the setup packet and/or transferred-byte count, invokes the existing host callback, and copies the transferred-byte count back to the caller. This preserves the existing public USB API and xHCI callback contract while removing CS0212 compiler errors.

The corrected source is mirrored into both SDK kernel template trees.
