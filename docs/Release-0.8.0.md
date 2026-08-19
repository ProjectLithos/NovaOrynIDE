# NovaOryn IDE 0.8.0

NovaOryn IDE 0.8.0 makes structured kernel telemetry an official SDK/kernel contract instead of an IDE convention inferred from console text.

## Structured kernel telemetry

The bundled SDK exposes five stable telemetry families through `KernelTelemetry`:

- `KernelTrace` for timestamped kernel activity and subsystem events.
- `KernelProfile` for sampled/profiled work and measured durations.
- `KernelBootEvent` for explicit boot-stage begin/end/warning/failure milestones.
- `KernelCounter` for monotonically reported or sampled kernel counters.
- `KernelDiagnosticEvent` for structured diagnostic codes and details.

Telemetry records carry a kind, subsystem, event name, nanosecond timestamp, two typed numeric values, correlation/sequence ID, detail text, CPU, thread ID and process ID. The runtime supports up to four telemetry sinks and records per-family and dropped-event statistics.

## IDE integration

The kernel bootstrap configures the console telemetry sink after the managed kernel runtime is ready. The stable wire protocol is consumed directly by the Tracing + Boot Analyser and Performance Profiler. Structured timestamps, phases, durations, CPU identity, counters and diagnostic severity are used directly rather than reconstructed from ordinary log messages wherever structured telemetry is available.

The existing text-milestone parser remains only as a compatibility fallback for kernels built before the structured telemetry contract.

## Version

This release bumps NovaOryn IDE from 0.7.7 to 0.8.0.
