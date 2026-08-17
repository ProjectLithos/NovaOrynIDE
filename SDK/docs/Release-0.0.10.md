# Nova Oryn OS SDK 0.0.10

## Purpose

Version 0.0.10 corrects NativeAOT/ILC acquisition after .NET SDK installation.

## Cause

`Microsoft.NETCore.App.Runtime.NativeAOT.win-x64` is a .NET runtime pack with NuGet package type `DotnetPlatform`. It must not be referenced directly as a normal application `PackageReference`. Doing so produces `NU1213`.

## Corrected behaviour

The bootstrap project now declares:

- `RuntimeIdentifier` as `win-x64`
- `SelfContained` as `true`
- `PublishAot` as `true`
- `RuntimeFrameworkVersion` as `10.0.10`
- `TargetLatestRuntimePatch` as `false`

The toolchain installer invokes `dotnet restore` with the matching RID and NativeAOT properties. The .NET SDK then resolves both `Microsoft.DotNet.ILCompiler` and the NativeAOT runtime pack through its framework/runtime-pack resolution model.

The runtime pack is no longer declared as a direct `PackageReference`.

## Expected continuation

The existing .NET SDK 10.0.302 installation is reused. The updater should resume at NativeAOT restoration without downloading .NET again.
