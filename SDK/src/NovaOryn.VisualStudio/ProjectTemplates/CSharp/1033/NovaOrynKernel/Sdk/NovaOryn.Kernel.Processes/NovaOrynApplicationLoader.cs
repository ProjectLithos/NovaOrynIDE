using System;
using NovaOryn.ApplicationFormat;

namespace NovaOryn.Kernel.Processes;

/// <summary>Bridges the NovaOryn .exe package container to the existing validated native image loader.</summary>
public static unsafe class NovaOrynApplicationLoader
{
    public const UInt16 SupportedAbiMajor=1;
    public const UInt16 SupportedAbiMinor=0;

    public static Boolean TryResolveNativeImage(Byte* packageOrImage,UInt64 length,out Byte* nativeImage,out UInt64 nativeLength,out NovaOrynApplicationInfo packageInfo,out Boolean packaged)
    {
        nativeImage=packageOrImage;nativeLength=length;packageInfo=default;packaged=false;if(packageOrImage==null||length==0UL)return false;
        if(!NovaOrynApplicationPackage.IsPackage(packageOrImage,length))return true;
        if(!NovaOrynApplicationPackage.TryInspect(packageOrImage,length,out packageInfo))return false;
        if(packageInfo.Architecture!=NovaOrynApplicationArchitecture.X64||packageInfo.AbiMajor!=SupportedAbiMajor||packageInfo.AbiMinor>SupportedAbiMinor)return false;
        if(!NovaOrynApplicationPackage.TryGetNativeImage(packageOrImage,length,packageInfo,out nativeImage,out nativeLength))return false;
        packaged=true;return true;
    }
}
