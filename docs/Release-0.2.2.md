# NovaOryn IDE 0.2.2

## Item 15: OS-specific Static Analyzers

NovaOryn IDE 0.2.2 adds a first-class OS analyzer service and Engineering view.

- `NOA1001` detects direct NovaOryn.Kernel references from userland.
- `NOA1002` flags unsafe/pointer userland implementation.
- `NOA2001` rejects managed blocking sleeps/delays in kernel and driver code.
- `NOA2002` flags managed exception failure paths in kernel and drivers.
- `NOA2003` flags managed Task/async use outside the freestanding scheduling contract.
- `NOA3001` detects raw port-I/O leakage outside architecture/HAL/driver layers.
- `NOA3002` detects direct architecture assembly use from generic OS code.
- `NOA3003` compares generic source against the active Target Manager architecture.
- `NOA4001` detects heap allocation inside interrupt/IRQ/ISR/exception handlers.
- `NOA5001` compares driver source hardware usage with `NovaOryn.Driver.json` capabilities.

The analyzer recursively scans OS-owned C# source while excluding generated build output, IDE metadata, and the bundled SDK. Results include stable rule IDs, severity, file/line/column, rule category, active architecture, and summary counts.
