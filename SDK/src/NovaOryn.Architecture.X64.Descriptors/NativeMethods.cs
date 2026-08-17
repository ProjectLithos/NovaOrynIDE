using System.Runtime.InteropServices;

namespace NovaOryn.Architecture.X64.Descriptors;

internal static partial class NativeMethods
{
    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64LoadGlobalDescriptorTable")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool LoadGlobalDescriptorTable(ulong tableAddress, ushort limit, ushort codeSelector, ushort dataSelector);

    [LibraryImport("__Internal", EntryPoint = "NovaOrynX64LoadTaskRegister")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool LoadTaskRegister(ushort selector);
}
