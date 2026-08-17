# NovaOryn 0.37.4

NovaOryn 0.37.4 replaces the unreliable Debug-build INT3 relocation anchor with a deterministic QEMU debug-console rendezvous. The x64 UEFI entry stub publishes its actual relocated runtime address through I/O port 0xE9, waits internally while the IDE arms source breakpoints, and exposes a linked resume symbol so execution can continue without a user-visible startup stop.

The optional Linux-kernel font installer also fixes PowerShell variable interpolation for cached font messages.
