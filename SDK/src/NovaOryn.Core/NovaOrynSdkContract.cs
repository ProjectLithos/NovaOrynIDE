namespace NovaOryn.Core;

/// <summary>Exposes the stable, versioned public contracts implemented by this NovaOryn SDK.</summary>
public static class NovaOrynSdkContract
{
    public const string SdkVersion = "0.38.0";
    public const string ApiVersion = "1.0";
    public const string AbiVersion = "1.0";
    public const string KernelAbiVersion = "1.0";
    public const string DriverAbiVersion = "1.0";
    public const string SyscallAbiVersion = "1.0";
    public const string DebugAbiVersion = "1.0";
    public const string CrashDumpAbiVersion = "1.0";
    public const string HeapDiagnosticsAbiVersion = "1.0";

    /// <summary>Returns true when a consumer API major version is compatible with this SDK.</summary>
    public static bool IsApiCompatible(int majorVersion) => majorVersion == 1;

    /// <summary>Returns true when a driver ABI major version is compatible with this SDK.</summary>
    public static bool IsDriverAbiCompatible(int majorVersion) => majorVersion == 1;
}
