# NovaOryn 0.17.0

NovaOryn 0.17.0 adds physical gigabit Ethernet drivers while retaining VirtIO-net as the first virtual network adapter.

- extends VirtIO-net with `PollAll()` for transport-independent receive servicing;
- adds `NovaOryn.Kernel.E1000` for supported Intel E1000/E1000e controllers;
- adds `NovaOryn.Kernel.Rtl8168` for RTL8168/RTL8111-class controllers;
- uses PMM-backed DMA descriptor and packet-buffer memory;
- registers every controller as an independent `KernelNetworking` interface;
- keeps controller registries dynamically heap-backed rather than imposing small device-count limits;
- deliberately leaves Intel I219/I225 ownership for a later dedicated family driver;
- adds independent E1000/E1000e and RTL8168/RTL8111 methodology tests;
- synchronizes the CLI and Visual Studio generated-kernel SDK trees.
