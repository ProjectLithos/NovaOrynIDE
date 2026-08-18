# NovaOryn IDE 0.4.0

NovaOryn IDE 0.4.0 advances the bundled SDK foundation with formal, versioned kernel subsystem contracts.

## Formal kernel subsystem contracts

The bundled SDK now publishes `NovaOryn.Kernel.SubsystemContracts` with contract version 1.0. It defines explicit public interfaces for Memory, Interrupts, Scheduler, Processes, Syscalls, Drivers, Filesystem, Networking, Graphics, Input, Time, Power and SMP.

A common `IKernelSubsystemContract` and `KernelSubsystemStatus` expose lifecycle state, version and capability information. Consumers can reject incompatible subsystem implementations by contract major/minor version instead of depending on implementation internals.

`NovaOryn.SubsystemContracts.json` is the machine-readable authoritative mapping from each subsystem boundary to its current implementation assembly. `NovaOryn.SdkManifest.json` advertises the subsystem contract version independently of SDK/API/ABI release versions.

The SDK documentation set now includes `Kernel-Subsystem-Contracts.md`, and the IDE build runs `Verify-NovaOrynIDESubsystemContracts.cjs` to prevent accidental omission of any of the thirteen formal boundaries.
