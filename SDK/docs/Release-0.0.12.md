# Nova Oryn OS SDK 0.0.13

## Purpose

Correct QEMU discovery after a successful winget installation.

## Changes

- Detects `qemu-system-x86_64.exe` through `PATH` and standard Weilnetz/winget installation locations.
- Persists resolved external tool paths in `.toolchain/NovaOryn.ToolPaths.json`.
- Reuses an existing QEMU 11 installation instead of reinstalling it.
- Applies equivalent path-resolution handling to NASM.
- Does not require QEMU's installer to update the current process environment.
