# Getting Started

Nova Oryn OS SDK compiles user-owned freestanding C# kernels with the repository-pinned .NET NativeAOT compiler.

## Create a kernel project

Install the Visual Studio extension with `Install-NovaOrynVSIX.bat`, then create a NovaOryn Kernel 0.23.0 project. The editable project is kept outside the SDK source tree.

## Kernel entry point

```csharp
[KernelEntry]
public static bool KMain(BootContext boot)
{
    // Initialise the facilities selected by your operating system.
    return CPU.Halt();
}
```

## Build and run

Use Ctrl+F5 to build and run without the debugger, or F5 for the NovaOryn debugging path. A normal command-line build uses `Build-NovaOryn.bat`; pass `-Run` through the PowerShell entry point when QEMU should start.

## Read the API reference

The Assemblies pages list every detected public item. Each API page shows its declaration, purpose, use guidance, dependencies, return contract, example and source location when those details exist in the source documentation.

## Kernel address-space policy

Generated 0.23.0 kernels initialize the standard x64 kernel address-space policy after the PMM and VMM. See `docs/Kernel-Address-Space-Design.md` for the reserved regions and custom-policy contract.

## Early allocator and kernel heap

Generated 0.23.0 kernels initialize the bounded early allocator and then the first-fit page-backed kernel heap after the address-space policy. The heap grows only within the standard kernel-heap virtual reservation and obtains physical backing from the PMM through VMM mappings. See `docs/Kernel-Heap.md`.

The framebuffer console now defaults to a 16-pixel glyph height, half the previous 32-pixel display size.

## ACPI and hardware discovery

Generated 0.23.0 kernels capture the ACPI RSDP before `ExitBootServices`, validate RSDP/RSDT/XSDT, then initialize reusable MADT, MCFG and HPET views plus FADT power management. `KernelAcpiEc` supports ECDT-described embedded controllers; `KernelAcpiPower` exposes fixed-feature power-button status, FADT reset, and AML `_S5`-driven shutdown. S1/S3/S4 transitions remain a later step. See `docs/ACPI-Hardware-Discovery.md` and `docs/ACPI-Platform-Drivers.md`.

## Timers and clocks in 0.23.0

NovaOryn 0.23.0 initializes HPET, explicit TSC capability detection, invariant-TSC calibration/selection, the calibrated Local APIC timer, and RTC/CMOS calendar reads. Generated `Kernel.cs` uses only `KernelTime`, `KernelHpet`, `KernelTsc`, `KernelLocalApicTimer`, and `KernelRtcCmos`; CPUID, RDTSC, CMOS port I/O, and timer MMIO remain SDK-owned. Startup prints the HPET frequency, TSC/invariant status and calibrated frequency, selected monotonic clock, Local APIC timer status/frequency, and a stable RTC/CMOS calendar sample. See `docs/Timers-and-Clocks.md`.

## SMP and per-CPU state in 0.23.0

After the early allocator is available, generated kernels initialize `KernelSmp`. The UEFI hand-off supplies a reserved SIPI page below 1 MiB, ACPI supplies enabled processor topology, and the x64 implementation starts xAPIC application processors with INIT/SIPI. Each AP switches to long mode using the active CR3, adopts its dedicated bootstrap stack, reports its APIC ID, and parks safely until the scheduler is implemented. The generated kernel prints SMP status, online/total processors, BSP index, and the trampoline address. See `docs/SMP-and-Per-CPU-State.md`.


## Scheduler and threads in 0.23.0

Generated kernels initialize `KernelScheduler` after SMP. The scheduler creates the bootstrap thread, owns per-thread kernel stacks and lifecycle state, exposes priority/affinity controls, and reports whether Local APIC timer preemption is available.

## User/kernel separation in 0.23.0

Generated kernels initialize `KernelProtection` after the scheduler. Ring-3 mappings are restricted to canonical lower-half user addresses, the hardware supervisor write-protect bit is enforced, SMEP is enabled when supported, and user transition contexts are validated without entering user mode before a syscall return path exists.

## Processes and executable loading in 0.23.0

Generated kernels initialize `KernelProcesses` after system calls. The loader supports validated in-memory x64 ELF64 `ET_EXEC` and PE32+ images, private user address spaces, user stacks, and ring-3 entry.

## Driver framework in 0.23.0

Generated kernels initialize `KernelDrivers` after the process facility and after `KernelHeap` is online. Driver and device tables start at 64/128 entries but grow dynamically from the kernel heap by default; `KernelDriverFrameworkOptions.Fixed(...)` is available when a deterministic build explicitly requires bounded capacity. Device enumerators can register devices and resources, drivers can match by bus/vendor/device/class and own probe/start/stop/remove callbacks, and interrupt requests use a transport-neutral broker rather than exposing PIC, I/O APIC, MSI, or MSI-X details to ordinary drivers.


## Storage and filesystems in 0.23.0

Generated kernels initialize generic `KernelStorage`/`KernelVfs` after `KernelDrivers` but install no filesystem provider. Filesystems are selected by the end user as separate kernel projects. The SDK includes the optional **NovaOryn Filesystem - FatFs** template for FAT12/FAT16/FAT32. Block devices remain independent and are registered through geometry plus read/write/flush callbacks. See `docs/Storage-and-Filesystems.md`.

## Networking in 0.23.0

Generated kernels initialise `KernelNetworking` after storage, then install VirtIO-net, Intel E1000/E1000e, and Realtek RTL8168/RTL8111-class adapters. All three feed the same adapter-neutral Ethernet/ARP/IPv4/ICMP/UDP/TCP stack. Intel I219/I225 remains reserved for the later dedicated family driver. See `docs/Networking.md` and `docs/Network-Adapters.md`.


## PCI / PCIe in 0.23.0

Generated kernels initialize `KernelPci` after the generic driver framework. PCIe uses ACPI MCFG ECAM when available and x64 otherwise supports conventional CF8/CFC configuration access. The PCI API enumerates devices/functions, reports vendor/device/class data, sizes/maps BARs, walks conventional and extended capabilities, and discovers MSI/MSI-X. See `docs/PCI-and-VirtIO.md`.

## VirtIO in 0.23.0

Generated kernels initialize `KernelVirtio` after storage and networking. The modern VirtIO PCI transport provides split virtqueues plus block, network, console, and RNG devices integrated with the existing driver, PMM/VMM, storage and networking layers. See `docs/PCI-and-VirtIO.md`.
