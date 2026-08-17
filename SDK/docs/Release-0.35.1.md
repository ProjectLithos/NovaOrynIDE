# NovaOryn 0.35.1

NovaOryn 0.35.1 is a Visual Studio template-policy correction based on 0.35.0.

## Visual Studio VSIX validation

- `Kernel/Kernel.cs` is validated as a high-level orchestration layer.
- `Boot/BootStartup.cs` is validated for descriptor, ACPI, memory, heap, SMP, scheduler, protection and syscall startup.
- `HAL/HardwareAbstractionLayer.cs` is validated for interrupt routing and device/subsystem initialization.
- The VSIX content audit now explicitly requires both `Boot/BootStartup.cs` and `HAL/HardwareAbstractionLayer.cs`.
- Obsolete policy checks no longer force boot and hardware initialization back into user-owned `Kernel.cs`.
