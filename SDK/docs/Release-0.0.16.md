# Nova Oryn OS SDK 0.0.16

## Purpose

Correct the managed solution configuration and remove the next deterministic build blockers found in the 0.0.15 x64 NativeAOT vertical slice.

## Changes

- Forces `Any CPU` when building the managed Visual Studio solution.
- Keeps x64 selection confined to NativeAOT publishing, NASM, LLD, and the kernel project metadata.
- Corrects the minimal-kernel manifest paths relative to its manifest directory.
- Rewrites QEMU argument construction using `ProcessStartInfo.ArgumentList`.
- Adds the initial `NovaOrynRuntimeInitialize` native object and links it explicitly.
- Adds source-policy checks for the managed solution platform and sample manifest path.

## Build

```bat
Build-NovaOryn.bat
```

The build now begins with `Release|Any CPU` for managed tools, then separately emits x64 native objects and the x64 NativeAOT static library.
