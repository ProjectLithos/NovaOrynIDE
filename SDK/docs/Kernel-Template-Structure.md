# Kernel template structure

New Visual Studio kernels keep `Kernel/Kernel.cs` intentionally high-level.

- `Kernel/Kernel.cs` orchestrates boot, HAL initialization and the interactive runtime.
- `Boot/BootStartup.cs` owns descriptor setup, ACPI/time/memory bootstrap, address spaces, heap, SMP, scheduler, protection and syscall initialization.
- `HAL/HardwareAbstractionLayer.cs` owns hardware discovery, buses, interrupt routing, input, storage, networking, graphics and device servicing.

This keeps boot and hardware-detection/configuration details out of the end-user kernel entry file.
