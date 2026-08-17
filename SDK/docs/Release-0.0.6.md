# Nova Oryn OS SDK 0.0.6

## Purpose

Version 0.0.6 adds the first post-commit toolchain bootstrap.

## Update order

`Update-NovaOryn.bat` now performs the required order:

1. select and extract FullSource for the first commit, or ChangedFiles thereafter
2. create the Git commit
3. push `main` to `origin`
4. stop without downloading tools if the push fails
5. run `Install-NovaOrynToolchain.bat` only after the push succeeds
6. retain and validate already-correct toolchain components
7. download only missing or invalid components

## Pinned components

- .NET SDK 10.0.302
- NativeAOT/ILC packages 10.0.10
- LLD and required LLVM utilities 22.1.6
- QEMU 11.0.0 or newer
- NASM 2.16.0 or newer

The full LLVM/Clang compiler is not used to compile the managed kernel. The LLVM Windows distribution is used to obtain LLD and the required binary utilities. The managed dependency graph is compiled by ILC.

## Toolchain location

Repository-local components are installed under:

```text
C:\NovaOryn\.toolchain
```

QEMU and NASM may use an existing valid installation. If absent, the installer uses `winget` and then validates that their executables are discoverable.

## Safety rules

- no toolchain download occurs before a successful GitHub push
- `.toolchain` is excluded from Git
- no kernel or operating-system image is created by these scripts
- toolchain installation is idempotent
