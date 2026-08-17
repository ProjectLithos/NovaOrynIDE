# NovaOryn 0.23.0

NovaOryn 0.23.0 adds the serial/debug transport layer.

- keeps the legacy 16550-compatible COM1 UART active from earliest kernel output;
- adds `NovaOryn.Kernel.Serial` with explicit capability/status reporting;
- discovers PCI communications-class serial controllers and configures standard 8250/16450/16550/16650/16750/16850/16950-compatible register interfaces over I/O or MMIO BARs;
- attaches started VirtIO console devices as post-boot debug mirrors;
- preserves COM1 and framebuffer output when a secondary serial transport fails;
- adds a non-blocking legacy UART receive primitive;
- adds an individual `NovaOryn.Serial.Tests` program and normal-build execution;
- synchronizes CLI and Visual Studio generated-kernel SDK trees and policy tests.

See `docs/Serial.md`.
