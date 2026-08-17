using System;
using NovaOryn.Kernel.Internal.X64;

namespace NovaOryn.Kernel.Platform.X64;

/// <summary>Provides high-level x64 processor initialization without exposing native I/O.</summary>
public static class KernelPlatform
{
    /// <summary>Installs the bootstrap processor GDT and TSS.</summary>
    public static Boolean InitializeDescriptors() => Native.InitializeBootstrapDescriptors();

    /// <summary>Installs the bootstrap processor IDT.</summary>
    public static Boolean InitializeInterrupts() => Native.InitializeBootstrapInterrupts();

    /// <summary>Disables legacy PIC delivery before APIC routing is configured.</summary>
    public static Boolean DisableLegacyPic() => Native.DisableLegacyPic();

    /// <summary>Stops the current processor permanently.</summary>
    public static Boolean Halt() => Native.Halt();
}
