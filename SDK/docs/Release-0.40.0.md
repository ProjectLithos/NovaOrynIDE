# NovaOryn SDK 0.40.0 — Structured kernel logging runtime

This release turns the structured logging contract introduced in SDK 0.39.0 into an active kernel runtime facility.

## Runtime logging

`KernelLog` now supports Trace, Debug, Info, Warning, Error and Critical levels, a configurable minimum level, up to four simultaneous sinks and per-level/dropped-record statistics. The original `Configure` API remains compatible.

The kernel bootstrap installs `KernelConsoleLogSink` once the kernel heap is online. Log lines emitted after that point contain the level, monotonic timestamp, CPU, current kernel thread, process field, subsystem, source and message. The process field remains zero for kernel-only threads until a process is formally associated with a scheduler thread.

Early pre-heap boot diagnostics deliberately retain the raw serial/framebuffer console as an emergency path. Structured logging must not make descriptor, interrupt, ACPI, memory-manager or heap bootstrap depend on managed sink allocation.

## Scheduler context

`KernelScheduler.TryGetCurrentThreadId` exposes the currently executing kernel thread to diagnostics and future telemetry without exposing scheduler internals.

## Migration

Major post-heap bootstrap milestones now use `KernelLog.Info`. Low-level human-readable value dumps continue to use `KernelConsole` where their output is presentation rather than a diagnostic event; later subsystem migrations can emit their own structured events without changing the logging ABI.

The structured logging contract is now version 1.1 in `NovaOryn.SdkManifest.json`.
