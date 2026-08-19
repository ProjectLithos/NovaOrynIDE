# NovaOryn IDE 0.8.1

NovaOryn IDE 0.8.1 is a corrective release for the first structured-kernel-telemetry integration.

## Freestanding telemetry link fix

0.8.0 used managed sink objects and interface dispatch for telemetry. NovaOryn's current freestanding NativeAOT kernel deliberately does not link the .NET GC/object-assignment and dynamic-interface-dispatch runtime helpers, so that design could compile to IL but failed during the native link with `RhpAssignRef`, `RhpCheckedAssignRef`, `RhpInitialDynamicInterfaceDispatch`, and `RhpNewFast` unresolved.

0.8.1 keeps the public telemetry event contracts while making the live kernel telemetry path allocation-free. The kernel registers static managed function pointers for the telemetry writer and context provider. No telemetry sink object is allocated, no managed reference is stored in the freestanding path, and no interface dispatch is required.

The five official event families remain `KernelTrace`, `KernelProfile`, `KernelBootEvent`, `KernelCounter`, and `KernelDiagnosticEvent`.

## Bottom-panel rendering fix

The Theia bottom content area is now an explicit Chromium paint-containment and isolation boundary. This prevents stale Output/Monaco glyph layers from being composited above the panel after scrolling or resizing.

## Kernel source structure

The generated `Kernel.cs` remains deliberately high-level. `BootStartup.Initialize` owns architecture, ACPI, time, memory, virtual memory, address space, heap, SMP, scheduler, protection, processes and system calls; `HardwareAbstractionLayer.Initialize` owns device discovery, PCI, interrupt grants, USB/input, storage, graphics and networking. The small entry point is therefore orchestration rather than evidence that those subsystems are unused.
