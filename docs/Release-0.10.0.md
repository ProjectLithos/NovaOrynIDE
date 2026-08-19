# NovaOryn IDE 0.10.0

## Kernel panic framework

NovaOryn now has an official freestanding-safe panic subsystem.

A panic contains reason/code, message, CPU, thread, process, register state and call stack. It can request a versioned NOCD crash dump, break into the attached debugger, and then apply a controlled halt or ACPI reboot policy.

The panic engine uses raw function pointers rather than a managed backend object, so the terminal path does not depend on GC write barriers or interface dispatch. The x64 debugger break uses `INT3`; the IDE recognizes it, captures the 1.0 NOCD dump automatically, and leaves the kernel paused so the developer can inspect it or Continue into the configured final policy.
