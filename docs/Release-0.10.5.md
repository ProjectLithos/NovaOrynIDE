# NovaOryn IDE 0.10.5

## Bottom panel controls

The normal Theia/Lumino bottom tab/control strip is now restored programmatically. NovaOryn watches the live bottom DockPanel and calls `show`, `update`, and `fit` on its real TabBar whenever bottom widgets are added. This fixes cases where CSS made the bar visible but Lumino itself still considered it hidden/collapsed.

The paint-containment fix remains confined to bottom content, preventing old Output text from painting over the editor.

## Devices and drivers

PCI discovery was already correct. The visible QEMU failure was the supported VirtIO GPU (`1AF4:1050`) failing during driver startup.

VirtIO GPU and generic VirtIO now enable PCI Memory Space and Bus Master (`PCI command |= 0x0006`) before BAR MMIO and virtqueue DMA.

The driver framework now has an authoritative `BindAndStartMatchingDevices()` reconciliation pass. Once configured driver families have registered, every discovered device with a matching driver receives a bind/start attempt. Devices for which NovaOryn has no selected driver—such as chipset bridges—remain correctly `Discovered`.

A driver that probes/binds successfully but has a transient Start failure remains `Bound` with a StartFailed diagnostic rather than being converted into an unrecoverable failed binding. A later reconciliation can retry it.
