# NovaOryn IDE 0.14.0

NovaOryn IDE 0.14.0 introduces formal hardware-abstraction boundaries for generated NovaOryn kernels.

## Hardware abstraction boundaries

- `NovaOryn.Kernel.Architecture` is the architecture-neutral kernel-facing API.
- `NovaOryn.Arch.X64` is the canonical x64 implementation boundary.
- `NovaOryn.Kernel.X64.LowLevel` remains a private implementation assembly below that boundary.
- Generated kernel projects no longer reference `NovaOryn.Kernel.X64.LowLevel` directly.
- x64 bootstrap/platform and panic transport now enter through `NovaOryn.Arch.X64`.
- `NovaOryn.ArchitectureBoundaries.json` records the dependency policy and reserves `NovaOryn.Arch.Arm64` as the future ARM64 implementation name.
- A 0.14.0 verifier rejects boundary regressions in generated kernel templates.

This preserves the existing x64 runtime while creating a stable generic API surface above architecture implementations. ARM64 can therefore be added later without requiring generic kernel consumers to be rewritten around ARM-specific details.
