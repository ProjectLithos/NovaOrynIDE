using System;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Storage;

namespace NovaOryn.Filesystem.FatFs;

public enum FatFsFormat : Byte { Unknown=0, Fat12=12, Fat16=16, Fat32=32 }

public readonly struct FatFsCapabilities
{
    public FatFsCapabilities(Boolean installed,Boolean fat12,Boolean fat16,Boolean fat32,Boolean exFat,Boolean longFileNames,Boolean read,Boolean write,Boolean extend,UInt32 maximumSectorSize)
    { Installed=installed;Fat12=fat12;Fat16=fat16;Fat32=fat32;ExFat=exFat;LongFileNames=longFileNames;Read=read;Write=write;Extend=extend;MaximumSectorSize=maximumSectorSize; }
    public Boolean Installed { get; } public Boolean Fat12 { get; } public Boolean Fat16 { get; } public Boolean Fat32 { get; }
    public Boolean ExFat { get; } public Boolean LongFileNames { get; } public Boolean Read { get; } public Boolean Write { get; }
    public Boolean Extend { get; } public UInt32 MaximumSectorSize { get; }
}

/// <summary>Selectable NovaOryn C# port of the FatFs model for FAT12/FAT16/FAT32.</summary>
public static unsafe class FatFs
{
    private const UInt32 MaximumSectorSize=65536U;
    private struct VolumeInfo
    {
        internal Byte Format,SectorsPerCluster,FatCount;
        internal UInt16 BytesPerSector,ReservedSectors,RootEntryCount;
        internal UInt32 SectorsPerFat,RootDirectorySectors,FirstRootSector,FirstDataSector,ClusterCount,RootCluster;
        internal UInt64 TotalSectors;
        internal Boolean ReadOnly;
    }
    private struct MountContext { internal KernelHeapAllocation Allocation; internal UInt32 Volume; internal VolumeInfo Info; }
    private struct FileContext
    {
        internal KernelHeapAllocation Allocation; internal UInt64 Mount; internal UInt32 FirstCluster;
        internal UInt64 Length,DirectorySector; internal UInt32 DirectoryOffset; internal Byte Type;
    }
    private static Boolean _installed;

    public static Boolean Install()
    {
        if(_installed)return true;
        if(!KernelVfs.IsInitialized())return false;
        KernelFileSystemCallbacks f12=new(&Probe12,&Mount12,&Unmount,&Open,&Read,&Write,&Flush,&Close);
        KernelFileSystemCallbacks f16=new(&Probe16,&Mount16,&Unmount,&Open,&Read,&Write,&Flush,&Close);
        KernelFileSystemCallbacks f32=new(&Probe32,&Mount32,&Unmount,&Open,&Read,&Write,&Flush,&Close);
        if(!KernelVfs.RegisterFileSystem(KernelFileSystemType.Fat12,f12))return false;
        if(!KernelVfs.RegisterFileSystem(KernelFileSystemType.Fat16,f16))return false;
        if(!KernelVfs.RegisterFileSystem(KernelFileSystemType.Fat32,f32))return false;
        _installed=true;return true;
    }
    public static FatFsCapabilities GetCapabilities()=>new(_installed,true,true,true,false,false,true,true,false,MaximumSectorSize);

    private static Boolean Probe12(KernelStorageVolumeHandle v)=>Probe(v,FatFsFormat.Fat12);
    private static Boolean Probe16(KernelStorageVolumeHandle v)=>Probe(v,FatFsFormat.Fat16);
    private static Boolean Probe32(KernelStorageVolumeHandle v)=>Probe(v,FatFsFormat.Fat32);
    private static Boolean Mount12(KernelStorageVolumeHandle v,UInt64* c)=>Mount(v,FatFsFormat.Fat12,c);
    private static Boolean Mount16(KernelStorageVolumeHandle v,UInt64* c)=>Mount(v,FatFsFormat.Fat16,c);
    private static Boolean Mount32(KernelStorageVolumeHandle v,UInt64* c)=>Mount(v,FatFsFormat.Fat32,c);

    private static Boolean Probe(KernelStorageVolumeHandle volume,FatFsFormat expected)
    {
        if(!TryReadBoot(volume,out VolumeInfo info,out KernelHeapAllocation scratch))return false;
        Boolean ok=info.Format==(Byte)expected;KernelHeap.TryRelease(scratch);return ok;
    }
    private static Boolean Mount(KernelStorageVolumeHandle volume,FatFsFormat expected,UInt64* cookie)
    {
        if(cookie==null||!TryReadBoot(volume,out VolumeInfo info,out KernelHeapAllocation scratch))return false;
        KernelHeap.TryRelease(scratch);if(info.Format!=(Byte)expected)return false;
        if(!KernelHeap.TryAllocate((UInt64)sizeof(MountContext),16,true,out KernelHeapAllocation allocation))return false;
        MountContext* context=(MountContext*)(nuint)allocation.Address;context->Allocation=allocation;context->Volume=volume.Value;context->Info=info;*cookie=allocation.Address;return true;
    }
    private static Boolean Unmount(UInt64 mountCookie)
    {
        if(mountCookie==0)return false;MountContext* mount=(MountContext*)(nuint)mountCookie;KernelHeapAllocation allocation=mount->Allocation;return KernelHeap.TryRelease(allocation);
    }
    private static Boolean Open(UInt64 mountCookie,String path,UInt32 pathOffset,KernelFileAccess access,UInt64* cookie,KernelFileType* type,UInt64* length)
    {
        if(mountCookie==0||path==null||cookie==null||type==null||length==null)return false;
        MountContext* mount=(MountContext*)(nuint)mountCookie;if(mount->Info.ReadOnly&&access!=KernelFileAccess.Read)return false;
        UInt32 cluster=mount->Info.Format==(Byte)FatFsFormat.Fat32?mount->Info.RootCluster:0U;
        UInt64 fileLength=0,entrySector=0;UInt32 entryOffset=0;KernelFileType fileType=KernelFileType.Directory;
        Int32 cursor=(Int32)pathOffset;while(cursor<path.Length&&path[cursor]=='/')cursor++;
        while(cursor<path.Length)
        {
            Int32 start=cursor;while(cursor<path.Length&&path[cursor]!='/')cursor++;Int32 count=cursor-start;
            if(count<=0||!FindEntry(mount,cluster,path,start,count,out UInt32 next,out fileType,out fileLength,out entrySector,out entryOffset))return false;
            cluster=next;while(cursor<path.Length&&path[cursor]=='/')cursor++;if(cursor<path.Length&&fileType!=KernelFileType.Directory)return false;
        }
        if(!KernelHeap.TryAllocate((UInt64)sizeof(FileContext),16,true,out KernelHeapAllocation allocation))return false;
        FileContext* file=(FileContext*)(nuint)allocation.Address;file->Allocation=allocation;file->Mount=mountCookie;file->FirstCluster=cluster;file->Length=fileLength;file->DirectorySector=entrySector;file->DirectoryOffset=entryOffset;file->Type=(Byte)fileType;
        *cookie=allocation.Address;*type=fileType;*length=fileLength;return true;
    }
    private static Boolean Read(UInt64 fileCookie,UInt64 position,Byte* buffer,UInt32 bytesToRead,UInt32* bytesRead)
    {
        if(fileCookie==0||buffer==null||bytesRead==null)return false;*bytesRead=0;
        FileContext* file=(FileContext*)(nuint)fileCookie;if(file->Type!=(Byte)KernelFileType.File||position>=file->Length||bytesToRead==0)return true;
        MountContext* mount=(MountContext*)(nuint)file->Mount;UInt64 remaining=file->Length-position;if(remaining>bytesToRead)remaining=bytesToRead;
        UInt32 clusterSize=(UInt32)mount->Info.BytesPerSector*(UInt32)mount->Info.SectorsPerCluster;UInt32 cluster=file->FirstCluster;if(cluster<2U)return false;
        for(UInt64 i=0;i<position/clusterSize;i++)if(!NextCluster(mount,cluster,out cluster)||IsEndOfChain(mount->Info,cluster))return false;
        UInt32 offset=(UInt32)(position%clusterSize);if(!KernelHeap.TryAllocate(clusterSize,16,false,out KernelHeapAllocation scratch))return false;Byte* data=(Byte*)(nuint)scratch.Address;UInt32 total=0;
        while(remaining>0&&!IsEndOfChain(mount->Info,cluster))
        {
            UInt64 sector=ClusterToSector(mount->Info,cluster);
            if(!KernelStorage.ReadVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),sector,mount->Info.SectorsPerCluster,data,clusterSize)){KernelHeap.TryRelease(scratch);return false;}
            UInt32 available=clusterSize-offset;UInt32 take=remaining<available?(UInt32)remaining:available;
            for(UInt32 i=0;i<take;i++)buffer[total+i]=data[offset+i];
            total+=take;remaining-=take;offset=0;if(remaining>0&&!NextCluster(mount,cluster,out cluster)){KernelHeap.TryRelease(scratch);return false;}
        }
        KernelHeap.TryRelease(scratch);*bytesRead=total;return true;
    }
    private static Boolean Write(UInt64 fileCookie,UInt64 position,Byte* buffer,UInt32 bytesToWrite,UInt32* bytesWritten)
    {
        if(fileCookie==0||buffer==null||bytesWritten==null)return false;*bytesWritten=0;
        FileContext* file=(FileContext*)(nuint)fileCookie;if(file->Type!=(Byte)KernelFileType.File||bytesToWrite==0)return true;
        MountContext* mount=(MountContext*)(nuint)file->Mount;if(mount->Info.ReadOnly||position>=file->Length)return false;
        UInt64 remaining=file->Length-position;if(remaining>bytesToWrite)remaining=bytesToWrite;
        UInt32 clusterSize=(UInt32)mount->Info.BytesPerSector*(UInt32)mount->Info.SectorsPerCluster;UInt32 cluster=file->FirstCluster;if(cluster<2U)return false;
        for(UInt64 i=0;i<position/clusterSize;i++)if(!NextCluster(mount,cluster,out cluster)||IsEndOfChain(mount->Info,cluster))return false;
        UInt32 offset=(UInt32)(position%clusterSize);if(!KernelHeap.TryAllocate(clusterSize,16,false,out KernelHeapAllocation scratch))return false;Byte* data=(Byte*)(nuint)scratch.Address;UInt32 total=0;
        while(remaining>0&&!IsEndOfChain(mount->Info,cluster))
        {
            UInt64 sector=ClusterToSector(mount->Info,cluster);
            if(!KernelStorage.ReadVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),sector,mount->Info.SectorsPerCluster,data,clusterSize)){KernelHeap.TryRelease(scratch);return false;}
            UInt32 available=clusterSize-offset;UInt32 take=remaining<available?(UInt32)remaining:available;
            for(UInt32 i=0;i<take;i++)data[offset+i]=buffer[total+i];
            if(!KernelStorage.WriteVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),sector,mount->Info.SectorsPerCluster,data,clusterSize)){KernelHeap.TryRelease(scratch);return false;}
            total+=take;remaining-=take;offset=0;if(remaining>0&&!NextCluster(mount,cluster,out cluster)){KernelHeap.TryRelease(scratch);return false;}
        }
        KernelHeap.TryRelease(scratch);*bytesWritten=total;return true;
    }
    private static Boolean Flush(UInt64 fileCookie)
    {
        if(fileCookie==0)return false;FileContext* file=(FileContext*)(nuint)fileCookie;MountContext* mount=(MountContext*)(nuint)file->Mount;
        if(!KernelStorage.TryGetVolume(new KernelStorageVolumeHandle(mount->Volume),out KernelStorageDeviceHandle device,out _))return false;return KernelStorage.Flush(device);
    }
    private static Boolean Close(UInt64 fileCookie)
    {
        if(fileCookie==0)return false;FileContext* file=(FileContext*)(nuint)fileCookie;KernelHeapAllocation allocation=file->Allocation;return KernelHeap.TryRelease(allocation);
    }
    private static Boolean FindEntry(MountContext* mount,UInt32 directoryCluster,String path,Int32 start,Int32 count,out UInt32 cluster,out KernelFileType type,out UInt64 length,out UInt64 entrySector,out UInt32 entryOffset)
    {
        cluster=0;type=KernelFileType.Unknown;length=0;entrySector=0;entryOffset=0;UInt32 bps=mount->Info.BytesPerSector;
        if(!KernelHeap.TryAllocate(bps,16,false,out KernelHeapAllocation scratch))return false;Byte* data=(Byte*)(nuint)scratch.Address;
        if(directoryCluster==0U&&mount->Info.Format!=(Byte)FatFsFormat.Fat32)
        {
            for(UInt32 s=0;s<mount->Info.RootDirectorySectors;s++)
            {
                UInt64 sector=(UInt64)mount->Info.FirstRootSector+s;
                if(!KernelStorage.ReadVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),sector,1,data,bps)){KernelHeap.TryRelease(scratch);return false;}
                if(FindEntryInSector(data,bps,path,start,count,out cluster,out type,out length,out UInt32 offset)){entrySector=sector;entryOffset=offset;KernelHeap.TryRelease(scratch);return true;}
                if(ContainsDirectoryTerminator(data,bps)){KernelHeap.TryRelease(scratch);return false;}
            }
            KernelHeap.TryRelease(scratch);return false;
        }
        UInt32 current=directoryCluster;
        while(current>=2U&&!IsEndOfChain(mount->Info,current))
        {
            UInt64 first=ClusterToSector(mount->Info,current);
            for(UInt32 s=0;s<mount->Info.SectorsPerCluster;s++)
            {
                UInt64 sector=first+s;
                if(!KernelStorage.ReadVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),sector,1,data,bps)){KernelHeap.TryRelease(scratch);return false;}
                if(FindEntryInSector(data,bps,path,start,count,out cluster,out type,out length,out UInt32 offset)){entrySector=sector;entryOffset=offset;KernelHeap.TryRelease(scratch);return true;}
                if(ContainsDirectoryTerminator(data,bps)){KernelHeap.TryRelease(scratch);return false;}
            }
            if(!NextCluster(mount,current,out current)){KernelHeap.TryRelease(scratch);return false;}
        }
        KernelHeap.TryRelease(scratch);return false;
    }
    private static Boolean FindEntryInSector(Byte* data,UInt32 bytes,String path,Int32 start,Int32 count,out UInt32 cluster,out KernelFileType type,out UInt64 length,out UInt32 entryOffset)
    {
        cluster=0;type=KernelFileType.Unknown;length=0;entryOffset=0;
        for(UInt32 offset=0;offset+32U<=bytes;offset+=32U)
        {
            Byte first=data[offset];if(first==0)return false;Byte attr=data[offset+11];if(first==0xE5||attr==0x0F||(attr&0x08)!=0)continue;
            if(!NameMatches(data+offset,path,start,count))continue;
            UInt32 high=Read16(data+offset+20),low=Read16(data+offset+26);cluster=(high<<16)|low;length=Read32(data+offset+28);
            type=(attr&0x10)!=0?KernelFileType.Directory:KernelFileType.File;entryOffset=offset;return true;
        }
        return false;
    }
    private static Boolean ContainsDirectoryTerminator(Byte* data,UInt32 bytes){for(UInt32 o=0;o+32U<=bytes;o+=32U)if(data[o]==0)return true;return false;}
    private static Boolean NameMatches(Byte* entry,String path,Int32 start,Int32 count)
    {
        Int32 dot=-1;for(Int32 i=0;i<count;i++)if(path[start+i]=='.'){dot=i;break;}Int32 baseCount=dot>=0?dot:count,extCount=dot>=0?count-dot-1:0;
        if(baseCount<1||baseCount>8||extCount>3)return false;
        for(Int32 i=0;i<8;i++){Char expected=i<baseCount?Upper(path[start+i]):' ';if((Char)entry[i]!=expected)return false;}
        for(Int32 i=0;i<3;i++){Char expected=i<extCount?Upper(path[start+dot+1+i]):' ';if((Char)entry[8+i]!=expected)return false;}
        return true;
    }
    private static Char Upper(Char c)=>c>='a'&&c<='z'?(Char)(c-32):c;
    private static Boolean NextCluster(MountContext* mount,UInt32 cluster,out UInt32 next)
    {
        next=0;UInt32 bps=mount->Info.BytesPerSector;
        UInt64 fatOffset=mount->Info.Format==(Byte)FatFsFormat.Fat12?(UInt64)cluster+(UInt64)(cluster/2U):mount->Info.Format==(Byte)FatFsFormat.Fat16?(UInt64)cluster*2UL:(UInt64)cluster*4UL;
        UInt64 fatSector=(UInt64)mount->Info.ReservedSectors+(fatOffset/bps);UInt32 offset=(UInt32)(fatOffset%bps);
        UInt32 required=(mount->Info.Format==(Byte)FatFsFormat.Fat12&&offset==bps-1U)?2U:1U,bytes=bps*required;
        if(!KernelHeap.TryAllocate(bytes,16,false,out KernelHeapAllocation scratch))return false;Byte* data=(Byte*)(nuint)scratch.Address;
        if(!KernelStorage.ReadVolumeBlocks(new KernelStorageVolumeHandle(mount->Volume),fatSector,required,data,bytes)){KernelHeap.TryRelease(scratch);return false;}
        if(mount->Info.Format==(Byte)FatFsFormat.Fat12){UInt16 pair=(UInt16)(data[offset]|((UInt16)data[offset+1U]<<8));next=(cluster&1U)==0U?(UInt32)(pair&0x0FFFU):(UInt32)(pair>>4);}
        else if(mount->Info.Format==(Byte)FatFsFormat.Fat16)next=Read16(data+offset);else next=Read32(data+offset)&0x0FFFFFFFU;
        KernelHeap.TryRelease(scratch);return true;
    }
    private static UInt64 ClusterToSector(VolumeInfo info,UInt32 cluster)=>cluster<2U?0UL:(UInt64)info.FirstDataSector+((UInt64)(cluster-2U)*(UInt64)info.SectorsPerCluster);
    private static Boolean IsEndOfChain(VolumeInfo info,UInt32 value)=>info.Format==(Byte)FatFsFormat.Fat12?value>=0x0FF8U:info.Format==(Byte)FatFsFormat.Fat16?value>=0xFFF8U:(value&0x0FFFFFFFU)>=0x0FFFFFF8U;

    private static Boolean TryReadBoot(KernelStorageVolumeHandle volume,out VolumeInfo info,out KernelHeapAllocation scratch)
    {
        info=default;scratch=default;
        if(!KernelStorage.TryGetVolume(volume,out KernelStorageDeviceHandle device,out _)||!KernelStorage.TryGetGeometry(device,out KernelStorageGeometry geometry)||geometry.LogicalBlockSize>MaximumSectorSize)return false;
        if(!KernelHeap.TryAllocate(geometry.LogicalBlockSize,16,false,out scratch))return false;Byte* data=(Byte*)(nuint)scratch.Address;
        if(!KernelStorage.ReadVolumeBlocks(volume,0,1,data,geometry.LogicalBlockSize)||!TryParseBootSector(data,geometry.LogicalBlockSize,geometry.ReadOnly,out info)){KernelHeap.TryRelease(scratch);scratch=default;return false;}
        return info.BytesPerSector==geometry.LogicalBlockSize;
    }

    public static Boolean TryParseBootSector(Byte* sector,UInt32 sectorBytes,Boolean readOnly,out FatFsFormat format,out UInt32 bytesPerSector,out UInt32 sectorsPerCluster,out UInt64 totalSectors)
    {
        format=FatFsFormat.Unknown;bytesPerSector=0;sectorsPerCluster=0;totalSectors=0;
        if(!TryParseBootSector(sector,sectorBytes,readOnly,out VolumeInfo info))return false;
        format=(FatFsFormat)info.Format;bytesPerSector=info.BytesPerSector;sectorsPerCluster=info.SectorsPerCluster;totalSectors=info.TotalSectors;return true;
    }
    private static Boolean TryParseBootSector(Byte* sector,UInt32 sectorBytes,Boolean readOnly,out VolumeInfo info)
    {
        info=default;if(sector==null||sectorBytes<512U||sector[510]!=0x55||sector[511]!=0xAA)return false;
        UInt16 bps=Read16(sector+11);Byte spc=sector[13];UInt16 reserved=Read16(sector+14);Byte fats=sector[16];UInt16 rootEntries=Read16(sector+17);
        UInt32 total=Read16(sector+19);if(total==0U)total=Read32(sector+32);UInt32 fatSectors=Read16(sector+22);if(fatSectors==0U)fatSectors=Read32(sector+36);
        if(bps<512U||bps>MaximumSectorSize||(bps&(bps-1U))!=0U||spc==0U||(spc&(spc-1U))!=0U||reserved==0U||fats==0U||fatSectors==0U||total==0U)return false;
        UInt32 rootDirSectors=((UInt32)rootEntries*32U+(UInt32)bps-1U)/(UInt32)bps;UInt64 nonData=(UInt64)reserved+((UInt64)fats*fatSectors)+rootDirSectors;if(nonData>=total)return false;
        UInt64 clusters=((UInt64)total-nonData)/spc;FatFsFormat format=clusters<4085UL?FatFsFormat.Fat12:clusters<65525UL?FatFsFormat.Fat16:FatFsFormat.Fat32;
        UInt32 rootCluster=format==FatFsFormat.Fat32?Read32(sector+44):0U;if(format==FatFsFormat.Fat32&&(rootEntries!=0U||rootCluster<2U))return false;if(format!=FatFsFormat.Fat32&&rootEntries==0U)return false;
        info.Format=(Byte)format;info.BytesPerSector=bps;info.SectorsPerCluster=spc;info.ReservedSectors=reserved;info.FatCount=fats;info.RootEntryCount=rootEntries;info.SectorsPerFat=fatSectors;
        info.RootDirectorySectors=rootDirSectors;info.FirstRootSector=(UInt32)((UInt64)reserved+(UInt64)fats*fatSectors);info.FirstDataSector=(UInt32)nonData;info.ClusterCount=clusters>UInt32.MaxValue?UInt32.MaxValue:(UInt32)clusters;
        info.RootCluster=rootCluster;info.TotalSectors=total;info.ReadOnly=readOnly;return true;
    }
    private static UInt16 Read16(Byte* p)=>(UInt16)(p[0]|((UInt16)p[1]<<8));
    private static UInt32 Read32(Byte* p)=>(UInt32)(p[0]|((UInt32)p[1]<<8)|((UInt32)p[2]<<16)|((UInt32)p[3]<<24));
}
