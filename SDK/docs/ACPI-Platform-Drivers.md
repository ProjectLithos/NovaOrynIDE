# ACPI Platform Drivers

NovaOryn 0.23.0 separates ACPI root-table discovery from higher-level platform services while keeping them in `NovaOryn.Kernel.Acpi`.

`KernelAcpi` remains the validated RSDP/RSDT/XSDT table-discovery foundation. `KernelAcpiMadt`, `KernelAcpiMcfg`, and `KernelAcpiHpet` provide reusable platform-driver views over MADT, MCFG, and HPET. `KernelAcpiFadt` parses fixed ACPI power-management registers and extended Generic Address Structures.

`KernelAcpiEc` supports ECDT-described embedded controllers using the standard EC read/write commands. EC devices that exist only in the AML namespace will be handled when NovaOryn grows a fuller AML namespace/interpreter layer.

`KernelAcpiPower` exposes power-button polling/status consumption, FADT-defined reset, and S5 shutdown. S5 types are taken from the DSDT `_S5` package rather than hard-coded for QEMU or a specific motherboard.

S1/S3/S4 sleep entry is intentionally deferred. Those states require broader AML method execution and platform device suspend/resume coordination, not only PM1 register writes.
