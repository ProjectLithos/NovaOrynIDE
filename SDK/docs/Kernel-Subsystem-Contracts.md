# Formal kernel subsystem contracts

NovaOryn defines one versioned public boundary for each major kernel subsystem. The first contract generation is `1.0` and is published by `NovaOryn.Kernel.SubsystemContracts`.

Kernel and driver code should depend on these contracts when crossing subsystem boundaries. Hardware-specific or implementation-specific entry points remain internal to their owning subsystem. This prevents consumers from binding themselves to allocator internals, APIC details, scheduler tables, filesystem implementations, NIC drivers, graphics transports, input buses, ACPI machinery, or SMP implementation details.

| Subsystem | Public contract | Current implementation |
|---|---|---|
| Memory | `IKernelMemoryContract` | `NovaOryn.Kernel.Memory` |
| Interrupts | `IKernelInterruptContract` | `NovaOryn.Kernel.InterruptBroker` |
| Scheduler | `IKernelSchedulerContract` | `NovaOryn.Kernel.Scheduler` |
| Processes | `IKernelProcessContract` | `NovaOryn.Kernel.Processes` |
| Syscalls | `IKernelSyscallContract` | `NovaOryn.Kernel.SystemCalls` |
| Drivers | `IKernelDriverContract` | `NovaOryn.Kernel.Drivers` |
| Filesystem | `IKernelFilesystemContract` | `NovaOryn.Kernel.Storage` |
| Networking | `IKernelNetworkingContract` | `NovaOryn.Kernel.Networking` |
| Graphics | `IKernelGraphicsContract` | `NovaOryn.Kernel.Graphics` |
| Input | `IKernelInputContract` | `NovaOryn.Kernel.Ps2` |
| Time | `IKernelTimeContract` | `NovaOryn.Kernel.Time` |
| Power | `IKernelPowerContract` | `NovaOryn.Kernel.Acpi` |
| SMP | `IKernelSmpContract` | `NovaOryn.Kernel.Smp` |

Every implementation reports `KernelSubsystemStatus` including the subsystem ID, lifecycle state, contract major/minor version and capability bits. Consumers use `IsCompatible` to require an exact major version and a minimum minor version. Breaking contract changes therefore require a new contract major version; additive changes can advance the minor version.

The machine-readable mapping is `NovaOryn.SubsystemContracts.json` and the SDK manifest advertises the subsystem-contract version independently of the SDK release/API/ABI versions.
