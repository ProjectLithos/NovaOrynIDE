using System;
using NovaOryn.Kernel.Drivers;

namespace $safeprojectname$;

public static unsafe class Driver
{
    public static Boolean Probe(KernelDriverDeviceContext* context) => context != null;
    public static Boolean Start(KernelDriverDeviceContext* context) => context != null;
    public static Boolean Stop(KernelDriverDeviceContext* context) => context != null;
    public static Boolean Remove(KernelDriverDeviceContext* context) => context != null;
    public static Boolean Interrupt(KernelDriverDeviceContext* context, UInt64 cookie) => context != null;
}
