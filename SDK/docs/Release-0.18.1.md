# NovaOryn 0.18.1

NovaOryn 0.18.1 is a compile-fix patch for the ACPI Embedded Controller platform service introduced in 0.18.0.

## Fixed

- Corrected the four ECDT Embedded Controller byte constants so their literals are `Byte`-compatible instead of explicitly `UInt32`-typed with the `U` suffix.
- Synchronized the correction across the authoritative SDK source, standalone generated-kernel template, and Visual Studio generated-kernel template.
- No ACPI runtime semantics or public API shapes were changed.

The affected constants are the standard EC read command (`0x80`), write command (`0x81`), output-buffer-full status bit (`0x01`), and input-buffer-full status bit (`0x02`).
