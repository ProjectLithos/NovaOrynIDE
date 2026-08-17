# NovaOryn 0.15.4

NovaOryn 0.15.4 is a corrective test-build release for the PCI/PCIe and VirtIO driver family introduced in 0.15.0.

## Correction

- Enables unsafe compilation for `NovaOryn.Pci.Tests`, matching `NovaOryn.Virtio.Tests`.
- The PCI methodology test directly compiles `KernelDriverContracts.cs`, which contains the intentionally unsafe `KernelDriverCallbacks` structure, so the test project must opt into `/unsafe`.
- No PCI or VirtIO runtime-driver API or behaviour is changed by this correction.

The 0.15.3 build completed the full NovaOryn solution and all policy, memory, timing, SMP, scheduler, protection, syscall, process, driver, storage, and networking tests before stopping only when the PCI/PCIe test project itself was compiled without `/unsafe`.
