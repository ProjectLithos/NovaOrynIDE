# NovaOryn 0.15.5

NovaOryn 0.15.5 is a PCI/PCIe runtime-enumeration correction release.

## Runtime correction

- Segment-zero conventional PCI configuration reads/writes below `0x100` now use x64 PCI Configuration Mechanism #1 (`CF8/CFC`) even when ACPI MCFG advertises ECAM.
- PCIe extended configuration space (`0x100`-`0xFFF`) continues to use MCFG ECAM.
- Non-zero PCI segments continue to use MCFG ECAM because legacy configuration mechanism #1 cannot address them.
- The discovered-device configuration transport is reported consistently with the conventional enumeration path.
- Generated kernels print `PCI/PCIe discovery starting.` before enumeration and `PCI/PCIe discovery complete.` afterwards so runtime boundaries are visible on serial and framebuffer consoles.
- PCI host tests now verify the transport-selection rules.

## Reason

On Q35 under QEMU TCG, the previous implementation preferred ECAM for every conventional configuration access whenever MCFG was present. Because NovaOryn mapped one 4 KiB ECAM function page at a time, scanning the firmware-advertised PCI bus range caused thousands of page-table map/unmap operations and could exceed the 30-second runtime acceptance window immediately after process initialization. The new policy preserves full ECAM support while making ordinary x64 enumeration use the efficient legacy configuration ports.
