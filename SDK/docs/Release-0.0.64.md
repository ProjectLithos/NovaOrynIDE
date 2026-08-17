# NovaOryn 0.0.64

This release fixes the native-link failure caused by duplicate x64 port-I/O exports.

- `Cpu.obj` remains the authoritative provider of `NovaOrynX64ReadPort8` and `NovaOrynX64WritePort8` for the bootstrap CPU/serial ABI.
- `InterruptControllers.obj` now exports the namespaced `NovaOrynX64ControllerReadPort8` and `NovaOrynX64ControllerWritePort8` symbols used by `NovaOryn.InterruptControllers.X64`.
- The managed interrupt-controller P/Invoke declarations now target those namespaced symbols.
- Source-policy tests reject duplicate native exports and verify that both native objects retain distinct port-I/O ABIs.

This resolves the LLD duplicate-symbol errors while preserving both the bootstrap kernel and driver-neutral interrupt-controller implementations.
