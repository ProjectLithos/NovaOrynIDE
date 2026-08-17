# NovaOryn 0.18.3

## Boot-policy synchronization fix

NovaOryn 0.18.3 corrects the boot-policy acceptance test after the ACPI platform-driver expansion.

The authoritative bootstrap and generated kernel templates report `ACPI MADT, MCFG, HPET, FADT and platform power services online.` The boot-policy test still required the older pre-platform-driver text `ACPI and hardware discovery online.`

The policy now validates the current ACPI platform milestone instead of requiring the obsolete message to be restored to the kernel. No ACPI runtime behavior or public API is changed by this patch.
