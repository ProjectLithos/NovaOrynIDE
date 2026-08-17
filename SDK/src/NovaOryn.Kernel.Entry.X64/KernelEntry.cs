using System;
using System.Runtime;
using NovaOryn.Kernel.Bootstrap;
using NovaOryn.Kernel.Console;

namespace NovaOryn.Kernel.Internal.X64;

internal static class KernelEntry
{
    [RuntimeExport("NovaOrynManagedEntry")]
    private static Boolean NativeEntry(UInt64 bootContextAddress)
    {
        return global::NovaOryn.Kernel.Bootstrap.Kernel.KMain(new BootContext(bootContextAddress));
    }
}
