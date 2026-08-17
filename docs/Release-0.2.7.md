# NovaOryn IDE 0.2.7

## Item 18 — Interrupt / APIC Visualiser

Adds a paused-debug visualiser for NovaOryn interrupt dispatch and the opaque x64 interrupt broker. It reads vector allocation/callback tables, exception-breakpoint state, Local APIC/x2APIC mode, I/O APIC GSI ranges, active I/O APIC/MSI/MSI-X routes and selected xAPIC registers directly from kernel memory.
