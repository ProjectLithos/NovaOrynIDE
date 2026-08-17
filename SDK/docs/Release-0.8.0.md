# NovaOryn 0.8.0

NovaOryn 0.8.0 implements roadmap item 15: scheduler and threads.

## Included
- Kernel thread lifecycle and four priority bands.
- Fixed-capacity thread table with stable IDs.
- Page-aligned per-thread kernel stacks.
- Per-CPU current-thread scheduling state linked through SMP scheduler contexts.
- Processor affinity for logical CPUs 0-63.
- Highest-priority runnable-thread selection.
- Cooperative yield and timer-preemption decision paths.
- Blocking, waking and termination operations.
- Configurable scheduler quantum with safe bounds.
- High-level bootstrap diagnostics and generated-project integration.
- Independent scheduler methodology tests.

Processes, user-mode context, executable loading and system calls remain later roadmap stages.
