# Nova Oryn OS SDK 0.0.11

## Purpose

Correct the repository-local LLVM toolchain validation after the official Windows distribution did not provide `llvm-mc.exe`.

## Changes

- Removes `llvm-mc.exe` from the mandatory LLVM tool list.
- Records `llvm-mc.exe` as an optional diagnostic tool.
- Continues to require `ld.lld.exe`, `lld-link.exe`, `llvm-ar.exe`, `llvm-objcopy.exe`, `llvm-readobj.exe`, `llvm-nm.exe`, and `llvm-objdump.exe`.
- Uses NASM as the required assembler for NovaOryn x64 native sources.
- Reuses the LLVM installation already downloaded into `.toolchain\LLVM`.
- Continues toolchain installation without redownloading valid components.

## Expected continuation

After extracting and committing this release, the toolchain installer should validate the existing LLVM installation and continue to QEMU and NASM checks.
