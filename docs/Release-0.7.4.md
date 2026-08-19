# NovaOryn IDE 0.7.4

Patch release fixing generated process-context support for structured kernel logging.

- Synchronizes `KernelProcesses` into the normal OS template and Visual Studio template.
- Generated kernels now expose `TryGetCurrentProcessId(out UInt64 processId)` and track the currently entered user process.
- Structured logging can include process context without failing generated Microkernel compilation.
- The kernel logging verifier now checks both generated process-runtime template copies, preventing SDK/template drift.
