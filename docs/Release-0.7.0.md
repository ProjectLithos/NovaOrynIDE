# NovaOryn IDE 0.7.0

NovaOryn IDE 0.7.0 completes the embedded SDK's structured kernel logging framework and makes it the normal path for kernel diagnostics.

Kernel records carry a severity (`Trace`, `Debug`, `Info`, `Warning`, `Error`, or `Critical`), subsystem, CPU, thread ID, process ID, monotonic timestamp in nanoseconds, source, and message. The console sink renders those fields in a stable machine-readable prefix while the runtime supports multiple sinks, minimum-level filtering, counters, and dropped-record accounting.

Logging is configured immediately after the kernel console becomes available, so early boot diagnostics no longer need to wait for heap or scheduler initialization. Context fields naturally begin at zero when the corresponding subsystem is not online and become live as SMP, scheduling, timekeeping, and processes initialize. User-process entry now records the current process ID so logs emitted while servicing that execution context identify the owning process.

The release retains direct console writes for interactive/presentation output such as command results and formatted capability summaries; milestone and diagnostic messages use the structured logging API.
