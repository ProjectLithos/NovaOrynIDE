# NovaOryn IDE 0.4.3

NovaOryn IDE 0.4.3 fixes generated operating systems that were still using the old minimal `Kernel.cs` bootstrap.

New IDE-created kernels now delegate to the SDK `BootStartup` and `HardwareAbstractionLayer` startup paths, initialize the command line and interrupt dispatch layer, and enter the interactive console instead of stopping after descriptor/interrupt setup.

The bundled SDK ProjectCreator also performs a deliberately narrow migration of the exact old IDE-generated minimal kernel. Existing user-written `Kernel.cs` files are not replaced. The migration is applied during the normal SDK project refresh before compilation, so existing NovaOryn IDE projects receive the full runtime bootstrap on their next build when they still contain that generated skeleton.

The full bootstrap includes physical and virtual memory, address-space/heap setup, ACPI/time, SMP, scheduler, user/kernel protection and NovaOryn/Linux/NT syscall initialization. The HAL then starts processes and all hardware/service families assigned to the kernel by the authoritative configuration, including input, drivers/PCI, storage, networking and USB.
