using System;

namespace NovaOryn.Kernel.Drivers;

public static class KernelDriverMath
{
    public static Boolean Matches(KernelDriverMatchRule rule, KernelDeviceIdentifier device)
    { if(rule.MatchBus&&rule.Bus!=device.Bus)return false; if(rule.MatchVendor&&rule.VendorId!=device.VendorId)return false; if(rule.MatchDevice&&rule.DeviceId!=device.DeviceId)return false; return rule.ClassMask==0U||(rule.ClassCode&rule.ClassMask)==(device.ClassCode&rule.ClassMask); }
    public static Boolean IsValidResource(KernelDeviceResource resource)
    { if(resource.Type==KernelDeviceResourceType.None)return false; if(resource.Length==0UL)return resource.Type==KernelDeviceResourceType.Interrupt; return resource.Start<=UInt64.MaxValue-(resource.Length-1UL); }
    public static Boolean IsValidInterruptRequest(KernelDriverInterruptRequest request) => request.Device.Value!=0U&&request.Priority<=15U;
    public static Boolean IsValidOptions(KernelDriverFrameworkOptions options)
    {
        if(options.InitialDriverCapacity==0U||options.InitialDeviceCapacity==0U)return false;
        if(options.InitialDriverCapacity>Int32.MaxValue||options.InitialDeviceCapacity>Int32.MaxValue)return false;
        if(options.MaximumDriverCapacity<options.InitialDriverCapacity||options.MaximumDeviceCapacity<options.InitialDeviceCapacity)return false;
        return true;
    }
    public static UInt32 NextCapacity(UInt32 current, UInt32 maximum)
    {
        if(current>=maximum)return current;
        UInt64 doubled=(UInt64)current*2UL; UInt64 result=doubled>maximum?maximum:doubled;
        if(result>Int32.MaxValue)result=Int32.MaxValue;
        return (UInt32)result;
    }
}
