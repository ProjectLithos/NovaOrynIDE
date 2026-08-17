# NovaOryn 0.12.1

NovaOryn 0.12.1 corrects roadmap item 19 so the normal driver/device registry uses the kernel heap that is already initialized before `KernelDrivers`.

## Changed

- `KernelDrivers.Initialize()` now creates a dynamically growing heap-backed registry.
- 64 driver slots and 128 device slots are now initial capacities, not limits.
- Full registries grow geometrically from `KernelHeap` while preserving existing handle slot positions.
- `KernelDriverFrameworkOptions` allows explicit initial and maximum capacities.
- `KernelDriverFrameworkOptions.Fixed(...)` retains deterministic bounded mode for RTOS and safety-oriented kernels.
- `KernelDriverCapabilities` now reports registry mode, current capacities, configured maximums, and usage.
- The driver assembly directly references `NovaOryn.Kernel.Heap`.
- Driver tests cover dynamic growth policy and fixed-mode configuration.

## Roadmap

Item 20 remains storage and filesystems.
