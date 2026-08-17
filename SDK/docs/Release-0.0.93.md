# NovaOryn 0.0.93

NovaOryn 0.0.93 corrects the source-policy test compilation failure in the 0.0.92 boot memory-map release.

## Source-policy build correction

`tests/NovaOryn.SourcePolicy.Tests/Program.cs` already declared `bootstrapBootContext` while validating final UEFI memory-map fields. A later framebuffer validation block attempted to declare another local with the same name in the same top-level scope, causing compiler error `CS0128` before the source-policy tests could run.

The framebuffer block now uses the purpose-specific local name `framebufferBootContext`. Its validation behaviour is unchanged: it still verifies that the freestanding boot context exposes the framebuffer address, size, scan-line width, pixel format, and direct-colour masks.

## Retained 0.0.92 functionality

This correction retains without redesign:

- final UEFI memory-map capture immediately before successful `ExitBootServices`;
- strict, safety-priority, and conservative memory-map normalisers;
- checked sorting, overflow rejection, overlap splitting, and compatible merging;
- kernel, boot-structure, framebuffer, MMIO, page-table, and early-allocation reservations;
- immutable diagnostic enumeration and future NUMA metadata.

## Validation target

The corrected source-policy test project must compile and execute before `NovaOryn.Memory.Tests`, managed NativeAOT compilation, native linking, image construction, and QEMU validation continue.
