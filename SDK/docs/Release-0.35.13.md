# NovaOryn 0.35.13

NovaOryn 0.35.13 fixes the VSIX build failure caused by Visual Studio MSBuild resolving `Microsoft.NET.Sdk` from the machine-wide .NET installation instead of NovaOryn's private toolchain.

The failure presented as:

- `Requested SDK version: 10.0.302`
- machine-wide SDK list containing only a different version such as `10.0.400`
- `MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found`

NovaOryn deliberately pins .NET SDK 10.0.302 in `global.json` and installs it under `.toolchain\DotNet`. The VSIX build must therefore use two components at once:

1. Visual Studio's own `MSBuild.exe`, so Microsoft.VSSDK.BuildTools can run the Visual Studio template-registration targets.
2. NovaOryn's repository-pinned .NET SDK, so SDK-style project imports resolve deterministically.

`Build-NovaOrynVSIX.ps1` now verifies the pinned `dotnet.exe`, reads the required SDK version from `global.json`, verifies the matching SDK payload, and temporarily configures the MSBuild SDK resolver with `DOTNET_ROOT`, `DOTNET_HOST_PATH`, `DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR`, `DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR`, `MSBuildSDKsPath`, and `DOTNET_MULTILEVEL_LOOKUP=0`.

The caller's environment is restored after MSBuild finishes.
