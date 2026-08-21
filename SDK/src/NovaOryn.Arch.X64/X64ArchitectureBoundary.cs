using System;
using NovaOryn.Kernel.Architecture;
using NovaOryn.Kernel.Internal.X64;

namespace NovaOryn.Arch.X64;

/// <summary>
/// Canonical x64 hardware-abstraction boundary. Generic kernel code must consume
/// NovaOryn.Kernel.Architecture or higher-level kernel services, never Native directly.
/// </summary>
public static class X64ArchitectureBoundary
{
    private static Boolean _installed;

    public static Boolean EnsureInstalled()
    {
        if (_installed) return true;
        KernelArchitectureCapabilities capabilities = new(true, true, true, true, true, true);
        if (!KernelArchitecture.Install(KernelArchitectureKind.X64, capabilities)) return false;
        _installed = true;
        return true;
    }

    public static Boolean InitializeDescriptors() => EnsureInstalled() && Native.InitializeBootstrapDescriptors();
    public static Boolean InitializeInterrupts() => EnsureInstalled() && Native.InitializeBootstrapInterrupts();
    public static Boolean DisableLegacyPic() => EnsureInstalled() && Native.DisableLegacyPic();
    public static Boolean Halt() => Native.Halt();

    public static Boolean CapturePanicContext(out UInt64 instructionPointer, out UInt64 stackPointer, out UInt64 framePointer, out UInt64 flags, out UInt64 pageTableRoot)
    {
        EnsureInstalled();
        return Native.CapturePanicContext(out instructionPointer, out stackPointer, out framePointer, out flags, out pageTableRoot);
    }

    public static Boolean PanicDebuggerBreak() => Native.PanicDebuggerBreak();
}
