# NovaOryn IDE 0.7.5

Patch release fixing the NativeAOT code-generation failure introduced by structured multi-sink kernel logging.

- Keeps the four-sink logging and telemetry contracts without managed interface-reference arrays during the no-GC bootstrap.
- Replaces `IKernelLogSink[]` and `IKernelTelemetrySink[]` storage with four fixed sink slots.
- Avoids forcing NativeAOT to require the full `System.Runtime.TypeCast` / reference-array store helper layer before NovaOryn has a managed runtime type system online.
- Preserves Trace/Debug/Info/Warning/Error/Critical levels, subsystem, CPU, thread/process, timestamp and source metadata.
- Synchronizes the canonical logging runtime into the normal OS and Visual Studio generated templates.
- Strengthens verification so generated templates cannot reintroduce reference-array sink storage.
