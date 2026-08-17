# NovaOryn 0.7.0

NovaOryn 0.7.0 implements roadmap item 14: SMP and per-CPU state.

The UEFI entry now reserves a 4 KiB application-processor SIPI page below 1 MiB before the final memory-map capture. That address is retained in the managed boot context. `NovaOryn.Kernel.Smp` builds a real-mode-to-long-mode x64 AP trampoline in that page, enumerates enabled ACPI processors, identifies the bootstrap processor, allocates per-CPU bootstrap stacks/state, performs xAPIC INIT/SIPI startup, validates an AP-reported APIC-ID handshake, and records successfully started APs as `OnlineParked`.

The APs intentionally remain parked with interrupts disabled after successful bootstrap. Processor startup and stable per-CPU ownership belong to item 14; thread dispatch, idle threads, run queues, preemption, and releasing APs into scheduled managed execution belong to roadmap item 15.

The release adds `KernelProcessorState`, `KernelSmpCapabilities`, explicit processor startup states, current-CPU lookup, a scheduler-context hand-off slot, and deterministic SIPI/state-layout helpers. Machines that cannot use the current xAPIC startup transport remain operational on the BSP and report the limitation instead of truncating x2APIC identifiers or claiming APs are online.

`NovaOryn.Smp.Tests` is an independent test program and is executed by `Build-NovaOryn.ps1` using the same explicit `Any CPU` output convention as the timer tests. Generated command-line and Visual Studio kernel projects contain the SMP SDK assembly and visibly report SMP status, online/total processor counts, BSP index, and the low-memory AP trampoline address.
