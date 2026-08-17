# NovaOryn IDE 0.1.49

NovaOryn IDE 0.1.49 adds professional kernel tracing/boot analysis and a performance profiler.

## 11 — Tracing + Boot Analyser

The **NovaOryn → Engineering → Tracing / Boot Analyser** view collects boot milestones from the kernel serial stream and accepts structured NovaOryn telemetry. It provides a live boot-stage timeline, event category filtering, CPU/event/duration information, bounded event history, and trace saving to `.novaoryn/traces/*.notrace.json`.

Structured telemetry format examples:

- `[NOVAORYN:TRACE] category=scheduler name=context-switch cpu=0`
- `[NOVAORYN:BOOT] stage="Drivers" phase=begin ms=12.4`
- `[NOVAORYN:BOOT] stage="Drivers" phase=end ms=15.9 status=complete`

The current SDK's existing human-readable boot milestones are recognised automatically, so the boot analyser is useful without changing user `Kernel.cs`.

## 12 — Performance Profiler

The **NovaOryn → Engineering → Performance Profiler** view aggregates boot-stage timings and structured profiler telemetry into hot-function/stage percentages, per-CPU utilisation, interrupt/syscall/scheduler counters, and duration/latency counters for heap, storage, network and other kernel subsystems.

Runtime telemetry examples:

- `[NOVAORYN:PROFILE] kind=sample cpu=0 function=KernelScheduler.Run category=cpu duration_ms=0.04`
- `[NOVAORYN:PROFILE] kind=counter category=interrupt name=irq14 delta=1`
- `[NOVAORYN:PROFILE] kind=counter category=storage name=nvme-read delta=1 duration_ms=0.31`

Profiler data can be reset independently while the OS continues running. Boot timings are collected automatically from normal NovaOryn boot output.
