# NovaOryn 0.15.0

NovaOryn 0.15.0 adds PCI/PCIe discovery and the first built-in VirtIO hardware drivers.

## PCI / PCIe

`NovaOryn.Kernel.Pci` now provides:

- legacy x64 PCI configuration-space access through CF8/CFC when ACPI does not advertise ECAM;
- PCIe ECAM discovery through the existing ACPI MCFG parser;
- segment/bus/device/function enumeration and registration in `KernelDrivers`;
- vendor ID, device ID, subsystem ID, revision and 24-bit class-code discovery;
- BAR decoding, standard BAR sizing, 32-bit memory BARs, 64-bit memory BARs and I/O BARs;
- device-MMIO mappings inside the standard `KernelAddressSpace.MmioBase` reservation using `KernelVirtualMemory` device protections;
- conventional PCI capability walking and PCIe extended-capability walking;
- MSI capability discovery; and
- MSI-X table/PBA capability discovery.

The driver registry remains heap-backed and dynamically growing. PCI discovery does not introduce fixed global driver/device limits.

## VirtIO PCI transport

`NovaOryn.Kernel.Virtio` implements the modern VirtIO PCI capability transport. It discovers common, notification, ISR and device configuration capabilities, negotiates `VIRTIO_F_VERSION_1`, configures split virtqueues, allocates physically contiguous queue memory through `KernelPhysicalMemory`, uses the kernel direct map for CPU access, and notifies queues through mapped PCI BAR regions.

The transport is installed as a normal `KernelDrivers` driver and binds supported `0x1AF4` PCI functions rather than bypassing the existing driver framework.

## Built-in VirtIO drivers

The release includes:

- **VirtIO block** — synchronous read, write and flush requests; 512-byte-sector capacity handling; optional device block size; read-only feature handling; and registration with `KernelStorage`.
- **VirtIO network** — receive and transmit split queues, MAC and MTU discovery, receive polling, and registration with `KernelNetworking`.
- **VirtIO console** — receive/transmit queues with high-level synchronous byte read/write APIs.
- **VirtIO RNG** — entropy queue with a high-level `FillRandom` API.

DMA buffers use PMM-owned physical pages rather than heap virtual addresses.

## Multi-device callback support

`KernelStorage` and `KernelNetworking` retain their existing callback APIs and now also expose contextual callback variants which receive the owning `KernelDeviceHandle`. This allows one driver implementation to serve multiple block devices or NICs without global single-device assumptions.

## x64 low-level ABI

The private x64 low-level assembly gains 32-bit I/O-port read/write exports used by legacy PCI configuration space. Kernel-facing PCI code remains in the dedicated PCI assembly; raw port I/O is not exposed in `Kernel.cs`.

## Bootstrap and templates

The generated kernel now initializes PCI immediately after the generic driver framework, then initializes storage/networking and starts VirtIO devices. Startup diagnostics report PCI device/ECAM counts and VirtIO block/network/console/RNG counts in human-readable decimal values.
