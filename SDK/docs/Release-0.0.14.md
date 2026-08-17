# NovaOryn 0.0.14

## Purpose

Corrects build-tool discovery for the first x64 NativeAOT build slice.

## Changes

- Reads the NASM path from all supported tool-path manifest keys, including the existing `nasm.exe` key.
- Falls back to standard NASM and WinGet installation locations.
- Reports the precise missing tool and all checked paths.
- Validates `dotnet`, `lld-link`, `llvm-nm`, and NASM independently.
- Prints resolved build-tool paths before compilation.
- Adds clearer failures for missing project manifests and missing NovaOryn tool outputs.

## Build

```bat
Build-NovaOryn.bat
```
