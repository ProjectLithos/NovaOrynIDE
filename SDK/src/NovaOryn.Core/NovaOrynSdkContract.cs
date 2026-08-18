namespace NovaOryn.Core;

/// <summary>Exposes the stable, versioned public contracts implemented by this NovaOryn SDK.</summary>
public static class NovaOrynSdkContract
{
    /// <summary>Gets the NovaOryn SDK product release version.</summary>
    public const string SdkVersion = "0.40.0";

    /// <summary>Gets the stable NovaOryn public API contract version.</summary>
    public const string ApiVersion = "1.1";

    /// <summary>Gets the overall NovaOryn binary ABI contract version.</summary>
    public const string AbiVersion = "1.0";

    /// <summary>Gets the NovaOryn kernel ABI contract version.</summary>
    public const string KernelAbiVersion = "1.0";

    /// <summary>Gets the NovaOryn driver ABI contract version.</summary>
    public const string DriverAbiVersion = "1.0";

    /// <summary>Gets the NovaOryn system-call ABI contract version.</summary>
    public const string SyscallAbiVersion = "1.0";

    /// <summary>Gets the NovaOryn debugger ABI contract version.</summary>
    public const string DebugAbiVersion = "1.0";

    /// <summary>Gets the NovaOryn crash-dump ABI contract version.</summary>
    public const string CrashDumpAbiVersion = "1.0";

    /// <summary>Gets the NovaOryn kernel-heap diagnostics ABI contract version.</summary>
    public const string HeapDiagnosticsAbiVersion = "1.0";

    /// <summary>Returns true when a consumer API major version is compatible with this SDK.</summary>
    /// <param name="majorVersion">The public API major version required by the consumer.</param>
    /// <returns><see langword="true"/> when the requested major version is supported; otherwise <see langword="false"/>.</returns>
    public static bool IsApiCompatible(int majorVersion) => majorVersion == 1;

    /// <summary>Returns true when a driver ABI major version is compatible with this SDK.</summary>
    /// <param name="majorVersion">The driver ABI major version required by the driver.</param>
    /// <returns><see langword="true"/> when the requested driver ABI major version is supported; otherwise <see langword="false"/>.</returns>
    public static bool IsDriverAbiCompatible(int majorVersion) => majorVersion == 1;
}
