# NovaOryn SDK 0.41.0

NovaOryn SDK 0.41.0 makes structured kernel telemetry a live runtime facility rather than a declaration-only contract.

`KernelTelemetry` now supports up to four sinks, live CPU/thread/process/time context, sequence correlation, per-kind counters and dropped-event statistics. The official event families are `KernelTrace`, `KernelProfile`, `KernelBootEvent`, `KernelCounter`, and `KernelDiagnosticEvent`.

The bootstrap configures `KernelConsoleTelemetrySink` after the kernel heap is online. It emits a stable debugger/serial protocol using `[NOVAORYN:TRACE]`, `[NOVAORYN:BOOT]`, and `[NOVAORYN:PROFILE]`. The IDE consumes nanosecond timestamps and durations directly, so its Tracing / Boot Analyser and Performance Profiler no longer need to infer post-heap kernel milestones from ordinary log text.

The telemetry contract is version 1.1. SDK API version is 1.2; the additions are backward-compatible with the previous telemetry event constructor and timestamp-explicit API overloads.
