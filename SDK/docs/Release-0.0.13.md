# Nova Oryn OS SDK 0.0.14

## Purpose

This release implements the first executable x64 NativeAOT/ILC compilation slice.

## Implemented

- real `dotnet publish` NativeAOT invocation with `NativeLib=Static`
- user-owned `bool KMain(BootContext)` reached through the blittable `NovaOrynManagedEntry` NativeAOT export
- validation that the produced native library contains the native bridge to `KMain`
- NASM-generated x64 COFF entry and CPU objects
- `lld-link` EFI application link orchestration
- permanent `cli`/`hlt` CPU halt loop
- `Build-NovaOryn.bat` and `Build-NovaOryn.ps1`
- corrected process argument handling without fragile quoted command strings

## Boundary of this release

This release implements and validates the real ILC object-generation and native-link path. The custom freestanding NativeAOT runtime initialization symbol and complete UEFI boot-services handover are the next implementation boundary. Until those runtime objects exist, the final EFI link may report unresolved NativeAOT/runtime symbols. That failure is intentional and explicit rather than being hidden by a substitute kernel.
