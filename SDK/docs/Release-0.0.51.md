# NovaOryn 0.0.51

This release introduces the first buildable architecture contracts and architecture-specific SDK assemblies.

## Added assemblies

- `NovaOryn.Core` now has an explicit build project for the shared architecture and boot metadata.
- `NovaOryn.Architecture.Contracts` defines architecture-neutral lifecycle, barrier, feature-query and page-table descriptor contracts.
- `NovaOryn.Architecture.X64` supplies the x64 lifecycle implementation and static native-bound hot-path operations.
- `NovaOryn.Architecture.Arm64` establishes the second-architecture assembly boundary while explicitly reporting that its native backend is not yet implemented.

## Dispatch policy

`ICpuArchitecture` is used for architecture selection, early initialisation and per-processor lifecycle operations. Performance-critical instructions are not routed through interface dispatch. `X64Operations` binds once to a complete table of unmanaged function pointers and then provides static operations without allocation or reflection.

## x64 ownership

The x64 architecture assembly now owns the public boundary for:

- control-register and model-specific-register access;
- interrupt enable and disable;
- processor halt and pause;
- timestamp reads;
- atomic compare/exchange;
- x64 port I/O;
- memory barriers;
- page-table entry encoding;
- context-switch assembly entry;
- CPU feature detection.

Exception-entry stubs remain native implementation assets and will bind into the interrupt subsystem when IDT work begins.

## Safety and compatibility

- Every public operation returns `bool` or a value.
- Unbound x64 operations fail predictably instead of executing an invalid function pointer.
- The ARM64 assembly never reports successful initialisation until a real native backend exists.
- Architecture-neutral page-table flags do not expose x64 bit positions to common memory-management code.
