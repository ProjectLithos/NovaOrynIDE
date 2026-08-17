using System;
using NovaOryn.Kernel.Drivers;

namespace NovaOryn.Kernel.Storage;

public enum KernelStorageRegistryMode : Byte { Dynamic=0, Fixed=1 }
public enum KernelStorageDeviceKind : Byte { Unknown=0, Physical=1, Virtual=2, RamDisk=3, Optical=4, UsbMassStorage=5 }
public enum KernelPartitionScheme : Byte { None=0, Raw=1, Mbr=2, Gpt=3 }
public enum KernelFileSystemType : Byte { Unknown=0, Fat12=1, Fat16=2, Fat32=3, ExFat=4, Custom=255 }
public enum KernelFileType : Byte { Unknown=0, File=1, Directory=2, Device=3 }
public enum KernelFileAccess : Byte { Read=1, Write=2, ReadWrite=3 }
public enum KernelSeekOrigin : Byte { Begin=0, Current=1, End=2 }
public enum KernelBlockOperation : Byte { Read=1, Write=2, Flush=3 }
public enum KernelStorageRequestState : Byte { Pending=1, Completed=2, Failed=3 }

public readonly struct KernelStorageOptions
{
    public KernelStorageOptions(KernelStorageRegistryMode mode,UInt32 initialDevices,UInt32 initialVolumes,UInt32 initialProviders,UInt32 initialNamespaces,UInt32 initialMounts,UInt32 initialOpenFiles,UInt32 initialRequests,UInt32 maximumDevices,UInt32 maximumVolumes,UInt32 maximumProviders,UInt32 maximumNamespaces,UInt32 maximumMounts,UInt32 maximumOpenFiles,UInt32 maximumRequests)
    { RegistryMode=mode;InitialDevices=initialDevices;InitialVolumes=initialVolumes;InitialProviders=initialProviders;InitialNamespaces=initialNamespaces;InitialMounts=initialMounts;InitialOpenFiles=initialOpenFiles;InitialRequests=initialRequests;MaximumDevices=maximumDevices;MaximumVolumes=maximumVolumes;MaximumProviders=maximumProviders;MaximumNamespaces=maximumNamespaces;MaximumMounts=maximumMounts;MaximumOpenFiles=maximumOpenFiles;MaximumRequests=maximumRequests; }
    public KernelStorageRegistryMode RegistryMode { get; } public UInt32 InitialDevices { get; } public UInt32 InitialVolumes { get; } public UInt32 InitialProviders { get; } public UInt32 InitialNamespaces { get; } public UInt32 InitialMounts { get; } public UInt32 InitialOpenFiles { get; } public UInt32 InitialRequests { get; }
    public UInt32 MaximumDevices { get; } public UInt32 MaximumVolumes { get; } public UInt32 MaximumProviders { get; } public UInt32 MaximumNamespaces { get; } public UInt32 MaximumMounts { get; } public UInt32 MaximumOpenFiles { get; } public UInt32 MaximumRequests { get; }
    public static KernelStorageOptions DynamicDefault => new(KernelStorageRegistryMode.Dynamic,16U,32U,8U,4U,16U,64U,64U,UInt32.MaxValue,UInt32.MaxValue,UInt32.MaxValue,UInt32.MaxValue,UInt32.MaxValue,UInt32.MaxValue,UInt32.MaxValue);
    public static KernelStorageOptions Fixed(UInt32 devices,UInt32 volumes,UInt32 providers,UInt32 namespaces,UInt32 mounts,UInt32 openFiles,UInt32 requests)=>new(KernelStorageRegistryMode.Fixed,devices,volumes,providers,namespaces,mounts,openFiles,requests,devices,volumes,providers,namespaces,mounts,openFiles,requests);
}


public readonly struct KernelStorageDeviceHandle { public KernelStorageDeviceHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelStorageVolumeHandle { public KernelStorageVolumeHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelMountNamespaceHandle { public KernelMountNamespaceHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelMountHandle { public KernelMountHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelFileHandle { public KernelFileHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelStorageRequestHandle { public KernelStorageRequestHandle(UInt32 value){Value=value;} public UInt32 Value { get; } }
public readonly struct KernelBlockRequest
{
    public KernelBlockRequest(KernelStorageDeviceHandle device,KernelBlockOperation operation,UInt64 firstBlock,UInt32 blockCount,UInt64 bufferAddress,UInt32 bufferBytes,UInt64 cookie) { Device=device;Operation=operation;FirstBlock=firstBlock;BlockCount=blockCount;BufferAddress=bufferAddress;BufferBytes=bufferBytes;Cookie=cookie; }
    public KernelStorageDeviceHandle Device { get; } public KernelBlockOperation Operation { get; } public UInt64 FirstBlock { get; } public UInt32 BlockCount { get; } public UInt64 BufferAddress { get; } public UInt32 BufferBytes { get; } public UInt64 Cookie { get; }
}
public readonly struct KernelStorageRequestInfo
{
    public KernelStorageRequestInfo(KernelStorageRequestHandle handle,KernelStorageRequestState state,KernelBlockRequest request) { Handle=handle;State=state;Request=request; }
    public KernelStorageRequestHandle Handle { get; } public KernelStorageRequestState State { get; } public KernelBlockRequest Request { get; }
}

public readonly struct KernelStorageGeometry
{
    public KernelStorageGeometry(UInt32 logicalBlockSize,UInt32 physicalBlockSize,UInt64 blockCount,Boolean readOnly,Boolean removable)
    { LogicalBlockSize=logicalBlockSize;PhysicalBlockSize=physicalBlockSize;BlockCount=blockCount;ReadOnly=readOnly;Removable=removable; }
    public UInt32 LogicalBlockSize { get; }
    public UInt32 PhysicalBlockSize { get; }
    public UInt64 BlockCount { get; }
    public Boolean ReadOnly { get; }
    public Boolean Removable { get; }
    public UInt64 CapacityBytes => (UInt64)LogicalBlockSize*BlockCount;
}

public readonly struct KernelPartitionInfo
{
    public KernelPartitionInfo(KernelPartitionScheme scheme,UInt32 index,UInt64 firstBlock,UInt64 blockCount,UInt32 mbrType,UInt64 typeGuidLow,UInt64 typeGuidHigh)
    { Scheme=scheme;Index=index;FirstBlock=firstBlock;BlockCount=blockCount;MbrType=mbrType;TypeGuidLow=typeGuidLow;TypeGuidHigh=typeGuidHigh; }
    public KernelPartitionScheme Scheme { get; }
    public UInt32 Index { get; }
    public UInt64 FirstBlock { get; }
    public UInt64 BlockCount { get; }
    public UInt32 MbrType { get; }
    public UInt64 TypeGuidLow { get; }
    public UInt64 TypeGuidHigh { get; }
}

public readonly struct KernelStorageCapabilities
{
    public KernelStorageCapabilities(Boolean initialized,KernelStorageRegistryMode mode,UInt32 devices,UInt32 volumes,UInt32 mounts,UInt32 openFiles,UInt32 queuedRequests,UInt32 deviceCapacity,UInt32 volumeCapacity,UInt32 mountCapacity,UInt32 openFileCapacity,UInt32 requestCapacity)
    { Initialized=initialized;RegistryMode=mode;Devices=devices;Volumes=volumes;Mounts=mounts;OpenFiles=openFiles;QueuedRequests=queuedRequests;DeviceCapacity=deviceCapacity;VolumeCapacity=volumeCapacity;MountCapacity=mountCapacity;OpenFileCapacity=openFileCapacity;RequestCapacity=requestCapacity; }
    public Boolean Initialized { get; }
    public KernelStorageRegistryMode RegistryMode { get; }
    public UInt32 Devices { get; }
    public UInt32 Volumes { get; }
    public UInt32 Mounts { get; }
    public UInt32 OpenFiles { get; }
    public UInt32 QueuedRequests { get; }
    public UInt32 DeviceCapacity { get; }
    public UInt32 VolumeCapacity { get; }
    public UInt32 MountCapacity { get; }
    public UInt32 OpenFileCapacity { get; }
    public UInt32 RequestCapacity { get; }
}

public readonly unsafe struct KernelBlockDeviceCallbacks
{
    public readonly delegate*<UInt64,UInt32,Byte*,UInt32,Boolean> ReadBlocks;
    public readonly delegate*<UInt64,UInt32,Byte*,UInt32,Boolean> WriteBlocks;
    public readonly delegate*<Boolean> Flush;
    public KernelBlockDeviceCallbacks(delegate*<UInt64,UInt32,Byte*,UInt32,Boolean> readBlocks,delegate*<UInt64,UInt32,Byte*,UInt32,Boolean> writeBlocks,delegate*<Boolean> flush)
    { ReadBlocks=readBlocks;WriteBlocks=writeBlocks;Flush=flush; }
}

/// <summary>Provides block callbacks that receive the owning generic device, allowing one driver implementation to serve multiple devices.</summary>
public readonly unsafe struct KernelContextualBlockDeviceCallbacks
{
    /// <summary>Reads blocks for the supplied generic device.</summary>
    public readonly delegate*<KernelDeviceHandle,UInt64,UInt32,Byte*,UInt32,Boolean> ReadBlocks;
    /// <summary>Writes blocks for the supplied generic device.</summary>
    public readonly delegate*<KernelDeviceHandle,UInt64,UInt32,Byte*,UInt32,Boolean> WriteBlocks;
    /// <summary>Flushes the supplied generic device.</summary>
    public readonly delegate*<KernelDeviceHandle,Boolean> Flush;
    /// <summary>Creates contextual block callbacks.</summary>
    public KernelContextualBlockDeviceCallbacks(delegate*<KernelDeviceHandle,UInt64,UInt32,Byte*,UInt32,Boolean> readBlocks,delegate*<KernelDeviceHandle,UInt64,UInt32,Byte*,UInt32,Boolean> writeBlocks,delegate*<KernelDeviceHandle,Boolean> flush)
    { ReadBlocks=readBlocks;WriteBlocks=writeBlocks;Flush=flush; }
}

public readonly unsafe struct KernelFileSystemCallbacks
{
    public readonly delegate*<KernelStorageVolumeHandle,Boolean> Probe;
    public readonly delegate*<KernelStorageVolumeHandle,UInt64*,Boolean> Mount;
    public readonly delegate*<UInt64,Boolean> Unmount;
    public readonly delegate*<UInt64,String,UInt32,KernelFileAccess,UInt64*,KernelFileType*,UInt64*,Boolean> Open;
    public readonly delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> Read;
    public readonly delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> Write;
    public readonly delegate*<UInt64,Boolean> Flush;
    public readonly delegate*<UInt64,Boolean> Close;
    public KernelFileSystemCallbacks(delegate*<KernelStorageVolumeHandle,Boolean> probe,delegate*<KernelStorageVolumeHandle,UInt64*,Boolean> mount,delegate*<UInt64,Boolean> unmount,delegate*<UInt64,String,UInt32,KernelFileAccess,UInt64*,KernelFileType*,UInt64*,Boolean> open,delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> read,delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> write,delegate*<UInt64,Boolean> flush,delegate*<UInt64,Boolean> close)
    { Probe=probe;Mount=mount;Unmount=unmount;Open=open;Read=read;Write=write;Flush=flush;Close=close; }
}

public readonly struct KernelVfsFileInfo
{
    public KernelVfsFileInfo(KernelFileHandle handle,KernelFileType type,UInt64 length,UInt64 position,KernelFileAccess access)
    { Handle=handle;Type=type;Length=length;Position=position;Access=access; }
    public KernelFileHandle Handle { get; }
    public KernelFileType Type { get; }
    public UInt64 Length { get; }
    public UInt64 Position { get; }
    public KernelFileAccess Access { get; }
}
