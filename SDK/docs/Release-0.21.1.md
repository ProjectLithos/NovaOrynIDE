# NovaOryn 0.21.1

NovaOryn 0.21.1 is a compile-correction release for the 0.21.0 opaque APIC/interrupt-broker implementation.

- Corrects the RTL8168 interrupt-mask literal to the register's `UInt16` width.
- Removes an unused `TableEntry` field from the interrupt broker route record; the MSI-X implementation currently uses broker-owned entry zero and does not retain an unused per-route field.
- No interrupt-broker policy or public driver abstraction is changed. Drivers continue to request opaque interrupts through `KernelDrivers`.
