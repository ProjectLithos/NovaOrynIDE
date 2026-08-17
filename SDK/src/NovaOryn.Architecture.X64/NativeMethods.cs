using System.Runtime.InteropServices;

namespace NovaOryn.Architecture.X64;

internal static partial class NativeMethods
{
    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64DisableInterrupts")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool DisableInterrupts();

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64EnableInterrupts")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool EnableInterrupts();

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64AreInterruptsEnabled")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool AreInterruptsEnabled();

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64Halt")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Halt();

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64WritePort8")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool WritePort8(ushort port, byte value);

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64ReadPort8")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool ReadPort8(ushort port, out byte value);
}
