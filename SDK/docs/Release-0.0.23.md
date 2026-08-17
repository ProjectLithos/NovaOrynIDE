# Nova Oryn OS SDK 0.0.23

## Purpose

This release fixes the no-CoreLib NativeAOT compiler-pack failure reported by `Microsoft.DotNet.ILCompiler.SingleEntry.targets`.

## Root cause

`dotnet publish -p:PublishAot=true` always enters the SDK NativeAOT runtime-pack resolution targets. The NovaOryn bootstrap deliberately removes the normal framework references, so the SDK could not create `ResolvedILCompilerPack` and stopped before invoking ILC.

## Correction

NovaOryn now owns the complete compilation sequence:

1. `dotnet build` compiles the custom no-standard-library C# project into managed IL.
2. `NovaOryn.ManagedCompiler.exe` invokes the repository-pinned `ilc.exe` directly.
3. ILC receives `--systemmodule NovaOryn.Kernel.Bootstrap`, `--targetos:win`, `--targetarch:x64`, and `--nativelib`.
4. ILC emits a single x64 COFF object.
5. `NovaOryn.Linker.exe` links that object with the NovaOryn UEFI entry, CPU, and bootstrap-runtime objects.

`targetos:win` selects the x64 PE/COFF ABI that UEFI uses. It does not introduce Windows CoreLib, Win32 imports, or Windows NativeAOT runtime libraries.

## Additional validation

- the toolchain installer verifies the runtime-specific ILC host package and records the exact `ilc.exe` path;
- the build script displays and passes that exact path;
- source-policy tests run during every build;
- tests reject any return to `dotnet publish` for the no-CoreLib bootstrap;
- tests require the linker to consume the direct ILC object.
