# NovaOryn SDK 0.41.0

NovaOryn SDK 0.41.0 makes structured kernel telemetry an active runtime and IDE-native event stream, while retaining the stable driver/kernel ABI.

## Professional SDK

`KernelTelemetry` now emits official trace, profile, boot, counter and diagnostic events with CPU/thread/process/time context and monotonic sequence IDs. The NovaOryn IDE Tracing + Boot Analyser and Performance Profiler consume the structured wire records directly.

See `NovaOryn.SdkManifest.json`, `NovaOryn.ApiContract.json`, `docs/Professional-SDK-0.39.0.md`, and `docs/Release-0.41.0.md`.
