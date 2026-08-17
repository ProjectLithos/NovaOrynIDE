# NovaOryn 0.0.1

## Purpose

Establish the clean repository and the first x64 NativeAOT vertical-slice contracts.

## Initial repository procedure

The initial `ProjectLithos/NovaOryn` commit shall be created from `NovaOryn-FullSource-0.0.1.zip`.

The archive shall be extracted directly into:

```text
C:\NovaOryn
```

The complete extracted source tree shall then be committed and pushed to:

```text
https://github.com/ProjectLithos/NovaOryn.git
```

No toolchain shall be downloaded before this first source commit is created and pushed. Toolchain content belongs under `.toolchain/`, is excluded from Git, and is installed only by an explicit NovaOryn executable command after the commit.

The ChangedFiles archive contains the same files in this first release only because the repository has no earlier source baseline. It is not the archive used to create the first commit.

## Included

- `KMain(BootContext)` entry contract and sample kernel
- typed primitives and boot context
- x64 CPU halt managed API
- x64 assembly entry and permanent `cli; hlt` loop
- serial-console contract and COM1 implementation contract
- project manifest model
- executable compiler, linker, image-builder, and QEMU launcher foundations
- source-policy validator
- release manifests and two ZIP layouts

## Deliberate limitations

This release has not been compiled inside the artifact-generation environment because the pinned .NET SDK and NativeAOT toolchain are not installed there. It is therefore source-complete for the stated foundation but not represented as a tested bootable SDK release.

The following remain for the next releases:

- validated `ilc` response-file generation against the pinned NativeAOT pack
- freestanding runtime helper implementation
- UEFI boot-context construction
- NativeAOT export binding from native entry to `KMain`
- final ELF and UEFI image validation
- QEMU boot acceptance test
