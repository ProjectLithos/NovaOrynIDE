# NovaOryn IDE 0.3.4

NovaOryn IDE 0.3.4 is a corrective release for the physical-machine debugger transport introduced in 0.3.3.

## Fixes

- Renames the Physical-machine Debugger widget target-selection handler so it no longer overrides Theia `ReactWidget.activate()`.
- Narrows the NativeAOT debug-map value immediately after loading it in both physical-machine and QEMU debug launch paths, satisfying TypeScript strict-null analysis without weakening runtime validation.
- Keeps the 0.3.3 physical GDB RSP transport behaviour and existing QEMU debugger path unchanged.
