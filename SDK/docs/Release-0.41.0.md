# NovaOryn SDK 0.41.0 — Structured kernel telemetry runtime

NovaOryn SDK 0.41.0 promotes structured telemetry from an API placeholder to an active kernel runtime and IDE wire protocol.

## Runtime

`KernelTelemetry` now supports up to four simultaneous sinks, a live CPU/thread/process/timestamp context provider, monotonic sequence IDs, per-kind/dropped statistics, and additive trace begin/end helpers. Existing SDK 0.39/0.40 telemetry constructors and timestamp-taking methods remain source-compatible.

The official event families are:

- `KernelTrace` / `KernelTraceBegin` / `KernelTraceEnd`
- `KernelProfile`
- `KernelBootEvent` / `KernelBootBegin` / `KernelBootEnd`
- `KernelCounter`
- `KernelDiagnosticEvent`

`KernelConsoleTelemetrySink` serialises events as the IDE-native `[NOVAORYN:TRACE]`, `[NOVAORYN:BOOT]`, and `[NOVAORYN:PROFILE]` records. The Tracing + Boot Analyser and Performance Profiler therefore consume official SDK events directly instead of inferring these events from arbitrary console text.

## Boot integration

Structured telemetry is activated after the kernel heap is online, using the same boot-safe context provider as structured logging. Boot stages for SMP/per-CPU, scheduler, protection, system calls, processes, and the driver framework are emitted explicitly. CPU and driver counts are emitted as counters.

Pre-heap diagnostics remain raw console output by design so early bootstrap cannot depend on allocation-backed diagnostics.

## Compatibility

- IDE: 0.4.6
- SDK: 0.41.0
- Public API contract: 1.2 (additive)
- Driver ABI: 1.0 (unchanged)
- Kernel telemetry contract: 1.1
