# NovaOryn IDE 0.11.13

NovaOryn IDE 0.11.13 corrects the comprehensive kernel presets introduced in 0.11.12.

- The start-page **Full Kernel** preset is renamed **Hybrid**.
- Hybrid, Microkernel and Monolithic now generate genuinely different `Kernel\Kernel.cs` sources.
- Generated kernels visibly call the public SDK initialization APIs appropriate to their architecture instead of all delegating to the same Boot/HAL wrapper pair.
- Microkernel keeps device/storage/network/USB responsibilities outside the kernel.
- Hybrid keeps the core plus selected latency-sensitive driver/input facilities kernel-resident.
- Monolithic directly initializes the full configured driver, storage, networking and USB stack in the kernel.
- `PUBLIC-SDK-USAGE.md` now documents the architecture-specific executable API usage.
