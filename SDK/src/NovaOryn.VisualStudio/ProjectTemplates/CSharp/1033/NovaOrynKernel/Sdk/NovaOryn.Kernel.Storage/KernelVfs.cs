using System;
using NovaOryn.Kernel.Heap;

namespace NovaOryn.Kernel.Storage;

/// <summary>
/// NovaOryn virtual filesystem. Owns namespaces, mount routing, handles, permissions and
/// synchronous file/directory I/O. Filesystem implementations are providers below this layer.
/// </summary>
public static unsafe class KernelVfs
{
    private struct ProviderRecord
    {
        internal Byte Used,Type;
        internal UInt32 Features;
        internal UInt64 Probe,Mount,Unmount,Open,Read,Write,Flush,Close,ReadDirectory,GetPermissions,SetPermissions;
    }
    private struct NamespaceRecord { internal Byte Used; }
    private struct MountRecord
    {
        internal Byte Used;
        internal UInt32 Namespace,Volume,Provider,PathLength;
        internal UInt64 MountCookie;
        internal KernelHeapAllocation PathAllocation;
    }
    private struct FileRecord
    {
        internal Byte Used,Type,Access;
        internal UInt32 Provider,Mount,Permissions;
        internal UInt64 Cookie,Position,Length,DirectoryIndex;
    }

    private static ProviderRecord* _providers;
    private static NamespaceRecord* _namespaces;
    private static MountRecord* _mounts;
    private static FileRecord* _files;
    private static KernelHeapAllocation _providerAllocation,_namespaceAllocation,_mountAllocation,_fileAllocation;
    private static UInt32 _providerCapacity,_namespaceCapacity,_mountCapacity,_fileCapacity,_providerCount,_namespaceCount,_mountCount,_openFileCount;
    private static UInt32 _maximumProviders,_maximumNamespaces,_maximumMounts,_maximumOpenFiles;
    private static KernelStorageRegistryMode _mode;
    private static Boolean _initialized;

    public static UInt32 ProviderCount=>_providerCount;
    public static UInt32 MountCount=>_mountCount;
    public static UInt32 OpenFileCount=>_openFileCount;
    public static UInt32 MountCapacity=>_mountCapacity;
    public static UInt32 OpenFileCapacity=>_fileCapacity;
    public static KernelMountNamespaceHandle DefaultNamespace=>new(1U);
    public static KernelVfsIoModel IoModel=>KernelVfsIoModel.Synchronous;
    public static Boolean SupportsAsyncIo=>false;

    internal static Boolean Initialize(KernelStorageOptions options)
    {
        if(_initialized)return true;
        _mode=options.RegistryMode;
        _providerCapacity=options.InitialProviders;_namespaceCapacity=options.InitialNamespaces;
        _maximumProviders=options.MaximumProviders;_maximumNamespaces=options.MaximumNamespaces;
        _maximumMounts=options.MaximumMounts;_maximumOpenFiles=options.MaximumOpenFiles;
        if(!AllocProviders(_providerCapacity,out _providerAllocation,out _providers)||
           !AllocNamespaces(_namespaceCapacity,out _namespaceAllocation,out _namespaces)||
           !AllocMounts(options.InitialMounts,out _mountAllocation,out _mounts)||
           !AllocFiles(options.InitialOpenFiles,out _fileAllocation,out _files))return false;
        _mountCapacity=options.InitialMounts;_fileCapacity=options.InitialOpenFiles;
        _namespaces->Used=1;_namespaceCount=1;_initialized=true;return true;
    }

    public static Boolean IsInitialized()=>_initialized;

    public static Boolean CreateNamespace(out KernelMountNamespaceHandle handle)
    {
        handle=default;if(!_initialized)return false;Int32 slot=FreeNamespace();
        if(slot<0){if(!GrowNamespaces())return false;slot=FreeNamespace();}
        (_namespaces+slot)->Used=1;_namespaceCount++;handle=new KernelMountNamespaceHandle((UInt32)slot+1U);return true;
    }

    /// <summary>Registers a filesystem driver beneath the VFS.</summary>
    public static Boolean RegisterFileSystem(KernelFileSystemType type,KernelFileSystemCallbacks callbacks)
    {
        if(!_initialized||type==KernelFileSystemType.Unknown||callbacks.Probe==null||callbacks.Mount==null||callbacks.Unmount==null||callbacks.Open==null||callbacks.Read==null||callbacks.Close==null)return false;
        if(FindProvider(type)>=0)return false;
        Int32 slot=FreeProvider();if(slot<0){if(!GrowProviders())return false;slot=FreeProvider();}
        ProviderRecord* p=_providers+slot;
        p->Used=1;p->Type=(Byte)type;p->Features=(UInt32)callbacks.Features;
        p->Probe=(UInt64)(void*)callbacks.Probe;p->Mount=(UInt64)(void*)callbacks.Mount;p->Unmount=(UInt64)(void*)callbacks.Unmount;
        p->Open=(UInt64)(void*)callbacks.Open;p->Read=(UInt64)(void*)callbacks.Read;p->Write=(UInt64)(void*)callbacks.Write;
        p->Flush=(UInt64)(void*)callbacks.Flush;p->Close=(UInt64)(void*)callbacks.Close;
        p->ReadDirectory=(UInt64)(void*)callbacks.ReadDirectory;p->GetPermissions=(UInt64)(void*)callbacks.GetPermissions;p->SetPermissions=(UInt64)(void*)callbacks.SetPermissions;
        _providerCount++;return true;
    }

    public static Boolean TryGetProviderInfo(KernelFileSystemType type,out KernelVfsProviderInfo info)
    {
        info=default;Int32 i=FindProvider(type);if(i<0)return false;ProviderRecord* p=_providers+i;
        info=new KernelVfsProviderInfo(type,(KernelFileSystemFeatures)p->Features,(UInt32)i+1U);return true;
    }

    public static Boolean Mount(KernelMountNamespaceHandle ns,KernelStorageVolumeHandle volume,KernelFileSystemType type,String path,out KernelMountHandle handle)
    {
        handle=default;if(!TryNamespace(ns)||volume.Value==0||!ValidAbsolutePath(path))return false;
        UInt32 pathLength=(UInt32)NormalizeMountLength(path);
        if(FindExactMount(ns,path,pathLength)>=0)return false;
        Int32 provider=FindProvider(type);if(provider<0)return false;ProviderRecord* p=_providers+provider;
        delegate*<KernelStorageVolumeHandle,Boolean> probe=(delegate*<KernelStorageVolumeHandle,Boolean>)(void*)p->Probe;if(!probe(volume))return false;
        UInt64 cookie=0;delegate*<KernelStorageVolumeHandle,UInt64*,Boolean> mount=(delegate*<KernelStorageVolumeHandle,UInt64*,Boolean>)(void*)p->Mount;if(!mount(volume,&cookie))return false;
        KernelHeapAllocation pathAllocation=default;if(!AllocateMountPath(path,pathLength,out pathAllocation)){CallUnmount(p,cookie);return false;}
        Int32 slot=FreeMount();if(slot<0){if(!GrowMounts()){KernelHeap.TryRelease(pathAllocation);CallUnmount(p,cookie);return false;}slot=FreeMount();}
        MountRecord* m=_mounts+slot;m->Used=1;m->Namespace=ns.Value;m->Volume=volume.Value;m->Provider=(UInt32)provider+1U;
        m->PathLength=pathLength;m->MountCookie=cookie;m->PathAllocation=pathAllocation;_mountCount++;handle=new KernelMountHandle((UInt32)slot+1U);return true;
    }

    public static Boolean Unmount(KernelMountHandle handle)
    {
        Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_mountCapacity||(_mounts+i)->Used==0)return false;
        for(Int32 f=0;f<(Int32)_fileCapacity;f++)if((_files+f)->Used!=0&&(_files+f)->Mount==handle.Value)return false;
        MountRecord* m=_mounts+i;ProviderRecord* p=_providers+(Int32)m->Provider-1;
        if(!CallUnmount(p,m->MountCookie))return false;KernelHeapAllocation pathAllocation=m->PathAllocation;Clear((Byte*)m,sizeof(MountRecord));_mountCount--;return KernelHeap.TryRelease(pathAllocation);
    }

    public static Boolean TryGetMountInfo(KernelMountHandle handle,out KernelVfsMountInfo info)
    {
        info=default;Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_mountCapacity)return false;MountRecord* m=_mounts+i;if(m->Used==0)return false;
        ProviderRecord* p=_providers+(Int32)m->Provider-1;
        info=new KernelVfsMountInfo(handle,new KernelMountNamespaceHandle(m->Namespace),new KernelStorageVolumeHandle(m->Volume),(KernelFileSystemType)p->Type,m->PathLength);return true;
    }

    public static Boolean Open(KernelMountNamespaceHandle ns,String path,KernelFileAccess access,out KernelFileHandle handle)
    {
        handle=default;if(!TryNamespace(ns)||!ValidAbsolutePath(path)||!ValidAccess(access))return false;
        Int32 mount=FindMount(ns,path);if(mount<0)return false;MountRecord* m=_mounts+mount;ProviderRecord* p=_providers+(Int32)m->Provider-1;
        UInt64 cookie=0,length=0;KernelFileType type=KernelFileType.Unknown;
        delegate*<UInt64,String,UInt32,KernelFileAccess,UInt64*,KernelFileType*,UInt64*,Boolean> open=(delegate*<UInt64,String,UInt32,KernelFileAccess,UInt64*,KernelFileType*,UInt64*,Boolean>)(void*)p->Open;
        if(!open(m->MountCookie,path,m->PathLength,access,&cookie,&type,&length))return false;
        KernelFilePermissions permissions=InferPermissions(p,type);
        TryProviderGetPermissions(p,m,path,&permissions);
        if(!PermissionsAllow(permissions,access)){CallClose(p,cookie);return false;}
        Int32 slot=FreeFile();if(slot<0){if(!GrowFiles()){CallClose(p,cookie);return false;}slot=FreeFile();}
        FileRecord* f=_files+slot;f->Used=1;f->Type=(Byte)type;f->Access=(Byte)access;f->Provider=m->Provider;f->Mount=(UInt32)mount+1U;
        f->Cookie=cookie;f->Length=length;f->Position=0;f->DirectoryIndex=0;f->Permissions=(UInt32)permissions;_openFileCount++;
        handle=new KernelFileHandle((UInt32)slot+1U);return true;
    }

    public static Boolean Read(KernelFileHandle handle,Byte* buffer,UInt32 bytesToRead,out UInt32 bytesRead)
    {
        bytesRead=0;if(!TryFile(handle,out FileRecord* f)||buffer==null||(f->Access!=(Byte)KernelFileAccess.Read&&f->Access!=(Byte)KernelFileAccess.ReadWrite))return false;
        if(bytesToRead==0)return true;if(f->Type!=(Byte)KernelFileType.File)return false;
        ProviderRecord* p=_providers+(Int32)f->Provider-1;delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> read=(delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean>)(void*)p->Read;
        UInt32 count=0;if(!read(f->Cookie,f->Position,buffer,bytesToRead,&count))return false;if(count>bytesToRead)return false;bytesRead=count;f->Position+=count;return true;
    }

    public static Boolean Write(KernelFileHandle handle,Byte* buffer,UInt32 bytesToWrite,out UInt32 bytesWritten)
    {
        bytesWritten=0;if(!TryFile(handle,out FileRecord* f)||buffer==null||(f->Access!=(Byte)KernelFileAccess.Write&&f->Access!=(Byte)KernelFileAccess.ReadWrite))return false;
        if(bytesToWrite==0)return true;if(f->Type!=(Byte)KernelFileType.File)return false;
        ProviderRecord* p=_providers+(Int32)f->Provider-1;if(p->Write==0)return false;
        delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean> write=(delegate*<UInt64,UInt64,Byte*,UInt32,UInt32*,Boolean>)(void*)p->Write;
        UInt32 count=0;if(!write(f->Cookie,f->Position,buffer,bytesToWrite,&count)||count>bytesToWrite)return false;bytesWritten=count;f->Position+=count;if(f->Position>f->Length)f->Length=f->Position;return true;
    }

    public static Boolean Seek(KernelFileHandle handle,Int64 offset,KernelSeekOrigin origin,out UInt64 position)
    {
        position=0;if(!TryFile(handle,out FileRecord* f)||f->Type!=(Byte)KernelFileType.File)return false;
        UInt64 basis=origin==KernelSeekOrigin.Begin?0UL:origin==KernelSeekOrigin.Current?f->Position:f->Length;
        if(offset<0){UInt64 amount=(UInt64)(-offset);if(amount>basis)return false;position=basis-amount;}
        else{UInt64 amount=(UInt64)offset;if(UInt64.MaxValue-basis<amount)return false;position=basis+amount;}
        f->Position=position;return true;
    }

    public static Boolean Flush(KernelFileHandle handle)
    {
        if(!TryFile(handle,out FileRecord* f))return false;ProviderRecord* p=_providers+(Int32)f->Provider-1;if(p->Flush==0)return true;
        delegate*<UInt64,Boolean> flush=(delegate*<UInt64,Boolean>)(void*)p->Flush;return flush(f->Cookie);
    }

    public static Boolean Close(KernelFileHandle handle)
    {
        if(!TryFile(handle,out FileRecord* f))return false;ProviderRecord* p=_providers+(Int32)f->Provider-1;if(!CallClose(p,f->Cookie))return false;
        Clear((Byte*)f,sizeof(FileRecord));_openFileCount--;return true;
    }

    public static Boolean OpenDirectory(KernelMountNamespaceHandle ns,String path,out KernelDirectoryHandle handle)
    {
        handle=default;if(!Open(ns,path,KernelFileAccess.Read,out KernelFileHandle file))return false;
        if(!TryFile(file,out FileRecord* f)||f->Type!=(Byte)KernelFileType.Directory){Close(file);return false;}
        ProviderRecord* p=_providers+(Int32)f->Provider-1;if(p->ReadDirectory==0){Close(file);return false;}
        handle=new KernelDirectoryHandle(file.Value);return true;
    }

    public static Boolean ReadDirectory(KernelDirectoryHandle handle,Char* nameBuffer,UInt32 nameCapacityChars,out UInt32 nameLength,out KernelFileType type,out UInt64 length,out KernelFilePermissions permissions)
    {
        nameLength=0;type=KernelFileType.Unknown;length=0;permissions=KernelFilePermissions.None;
        KernelFileHandle file=new(handle.Value);if(!TryFile(file,out FileRecord* f)||f->Type!=(Byte)KernelFileType.Directory||nameBuffer==null||nameCapacityChars==0)return false;
        ProviderRecord* p=_providers+(Int32)f->Provider-1;if(p->ReadDirectory==0)return false;
        delegate*<UInt64,UInt64,Char*,UInt32,UInt32*,KernelFileType*,UInt64*,KernelFilePermissions*,Boolean> readDirectory=(delegate*<UInt64,UInt64,Char*,UInt32,UInt32*,KernelFileType*,UInt64*,KernelFilePermissions*,Boolean>)(void*)p->ReadDirectory;
        UInt32 n=0;KernelFileType t=KernelFileType.Unknown;UInt64 l=0;KernelFilePermissions perms=KernelFilePermissions.None;
        if(!readDirectory(f->Cookie,f->DirectoryIndex,nameBuffer,nameCapacityChars,&n,&t,&l,&perms))return false;
        if(n>=nameCapacityChars)return false;nameLength=n;type=t;length=l;permissions=perms;f->DirectoryIndex++;return true;
    }

    public static Boolean RewindDirectory(KernelDirectoryHandle handle)
    {
        KernelFileHandle file=new(handle.Value);if(!TryFile(file,out FileRecord* f)||f->Type!=(Byte)KernelFileType.Directory)return false;f->DirectoryIndex=0;return true;
    }

    public static Boolean CloseDirectory(KernelDirectoryHandle handle)=>Close(new KernelFileHandle(handle.Value));

    public static Boolean TryGetPermissions(KernelMountNamespaceHandle ns,String path,out KernelFilePermissions permissions)
    {
        permissions=KernelFilePermissions.None;if(!TryNamespace(ns)||!ValidAbsolutePath(path))return false;Int32 mount=FindMount(ns,path);if(mount<0)return false;
        MountRecord* m=_mounts+mount;ProviderRecord* p=_providers+(Int32)m->Provider-1;if(p->GetPermissions==0)return false;
        delegate*<UInt64,String,UInt32,KernelFilePermissions*,Boolean> get=(delegate*<UInt64,String,UInt32,KernelFilePermissions*,Boolean>)(void*)p->GetPermissions;
        KernelFilePermissions value=KernelFilePermissions.None;if(!get(m->MountCookie,path,m->PathLength,&value))return false;permissions=value;return true;
    }

    public static Boolean TrySetPermissions(KernelMountNamespaceHandle ns,String path,KernelFilePermissions permissions)
    {
        if(!TryNamespace(ns)||!ValidAbsolutePath(path))return false;Int32 mount=FindMount(ns,path);if(mount<0)return false;
        MountRecord* m=_mounts+mount;ProviderRecord* p=_providers+(Int32)m->Provider-1;if(p->SetPermissions==0)return false;
        delegate*<UInt64,String,UInt32,KernelFilePermissions,Boolean> set=(delegate*<UInt64,String,UInt32,KernelFilePermissions,Boolean>)(void*)p->SetPermissions;
        return set(m->MountCookie,path,m->PathLength,permissions);
    }

    public static Boolean TryGetFileInfo(KernelFileHandle handle,out KernelVfsFileInfo info)
    {
        info=default;if(!TryFile(handle,out FileRecord* f))return false;
        info=new KernelVfsFileInfo(handle,(KernelFileType)f->Type,f->Length,f->Position,(KernelFileAccess)f->Access,(KernelFilePermissions)f->Permissions);return true;
    }

    private static KernelFilePermissions InferPermissions(ProviderRecord* p,KernelFileType type)
    {
        KernelFilePermissions value=KernelFilePermissions.OwnerRead|KernelFilePermissions.GroupRead|KernelFilePermissions.OtherRead;
        if((p->Features&(UInt32)KernelFileSystemFeatures.Write)!=0)value|=KernelFilePermissions.OwnerWrite;
        if(type==KernelFileType.Directory)value|=KernelFilePermissions.OwnerExecute|KernelFilePermissions.GroupExecute|KernelFilePermissions.OtherExecute;
        return value;
    }
    private static Boolean PermissionsAllow(KernelFilePermissions p,KernelFileAccess access)
    {
        Boolean read=(p&(KernelFilePermissions.OwnerRead|KernelFilePermissions.GroupRead|KernelFilePermissions.OtherRead))!=0;
        Boolean write=(p&KernelFilePermissions.OwnerWrite)!=0&&(p&KernelFilePermissions.ReadOnly)==0;
        return access==KernelFileAccess.Read?read:access==KernelFileAccess.Write?write:read&&write;
    }
    private static void TryProviderGetPermissions(ProviderRecord* p,MountRecord* m,String path,KernelFilePermissions* permissions)
    {
        if(p->GetPermissions==0)return;delegate*<UInt64,String,UInt32,KernelFilePermissions*,Boolean> get=(delegate*<UInt64,String,UInt32,KernelFilePermissions*,Boolean>)(void*)p->GetPermissions;
        KernelFilePermissions value=*permissions;if(get(m->MountCookie,path,m->PathLength,&value))*permissions=value;
    }
    private static Boolean CallUnmount(ProviderRecord* p,UInt64 cookie){delegate*<UInt64,Boolean> fn=(delegate*<UInt64,Boolean>)(void*)p->Unmount;return fn(cookie);}
    private static Boolean CallClose(ProviderRecord* p,UInt64 cookie){delegate*<UInt64,Boolean> fn=(delegate*<UInt64,Boolean>)(void*)p->Close;return fn(cookie);}
    private static Boolean ValidAccess(KernelFileAccess a)=>a==KernelFileAccess.Read||a==KernelFileAccess.Write||a==KernelFileAccess.ReadWrite;
    private static Boolean ValidAbsolutePath(String path)=>path!=null&&path.Length>0&&path[0]=='/'&&path.IndexOf('\\')<0;
    private static Int32 FindProvider(KernelFileSystemType type){for(Int32 i=0;i<(Int32)_providerCapacity;i++)if((_providers+i)->Used!=0&&(_providers+i)->Type==(Byte)type)return i;return -1;}
    private static Int32 FindExactMount(KernelMountNamespaceHandle ns,String path,UInt32 length){for(Int32 i=0;i<(Int32)_mountCapacity;i++){MountRecord* m=_mounts+i;if(m->Used!=0&&m->Namespace==ns.Value&&m->PathLength==length&&MountPathEquals(m,path,length))return i;}return -1;}
    private static Int32 FindMount(KernelMountNamespaceHandle ns,String path){Int32 best=-1;UInt32 bestLength=0;for(Int32 i=0;i<(Int32)_mountCapacity;i++){MountRecord* m=_mounts+i;if(m->Used==0||m->Namespace!=ns.Value||m->PathLength>(UInt32)path.Length||m->PathLength<bestLength)continue;if(!MountPathEquals(m,path,m->PathLength))continue;if(m->PathLength>1&&path.Length>m->PathLength&&path[(Int32)m->PathLength]!='/')continue;best=i;bestLength=m->PathLength;}return best;}
    private static Boolean MountPathEquals(MountRecord* m,String path,UInt32 length){if(m->PathAllocation.Address==0||length!=m->PathLength)return false;Char* saved=(Char*)(nuint)m->PathAllocation.Address;for(UInt32 i=0;i<length;i++){Char a=saved[i],b=path[(Int32)i];if(a>='A'&&a<='Z')a=(Char)(a+32);if(b>='A'&&b<='Z')b=(Char)(b+32);if(a!=b)return false;}return true;}
    private static Boolean AllocateMountPath(String path,UInt32 length,out KernelHeapAllocation allocation){allocation=default;UInt64 bytes=((UInt64)length+1UL)*2UL;if(!KernelHeap.TryAllocate(bytes,16,true,out allocation))return false;Char* d=(Char*)(nuint)allocation.Address;for(UInt32 i=0;i<length;i++)d[i]=path[(Int32)i];d[length]='\0';return true;}
    private static Int32 NormalizeMountLength(String path){Int32 n=path.Length;while(n>1&&path[n-1]=='/')n--;return n;}
    private static Boolean TryNamespace(KernelMountNamespaceHandle h){Int32 i=(Int32)h.Value-1;return _initialized&&i>=0&&(UInt32)i<_namespaceCapacity&&(_namespaces+i)->Used!=0;}
    private static Boolean TryFile(KernelFileHandle h,out FileRecord* f){f=null;Int32 i=(Int32)h.Value-1;if(!_initialized||i<0||(UInt32)i>=_fileCapacity||(_files+i)->Used==0)return false;f=_files+i;return true;}
    private static Int32 FreeProvider(){for(Int32 i=0;i<(Int32)_providerCapacity;i++)if((_providers+i)->Used==0)return i;return -1;}
    private static Int32 FreeNamespace(){for(Int32 i=0;i<(Int32)_namespaceCapacity;i++)if((_namespaces+i)->Used==0)return i;return -1;}
    private static Int32 FreeMount(){for(Int32 i=0;i<(Int32)_mountCapacity;i++)if((_mounts+i)->Used==0)return i;return -1;}
    private static Int32 FreeFile(){for(Int32 i=0;i<(Int32)_fileCapacity;i++)if((_files+i)->Used==0)return i;return -1;}
    private static Boolean GrowProviders(){if(_mode!=KernelStorageRegistryMode.Dynamic)return false;UInt32 next=KernelStorageMath.NextCapacity(_providerCapacity,_maximumProviders);if(!AllocProviders(next,out KernelHeapAllocation a,out ProviderRecord* n))return false;Copy((Byte*)_providers,(Byte*)n,(UInt64)_providerCapacity*(UInt64)sizeof(ProviderRecord));KernelHeapAllocation old=_providerAllocation;_providerAllocation=a;_providers=n;_providerCapacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean GrowNamespaces(){if(_mode!=KernelStorageRegistryMode.Dynamic)return false;UInt32 next=KernelStorageMath.NextCapacity(_namespaceCapacity,_maximumNamespaces);if(!AllocNamespaces(next,out KernelHeapAllocation a,out NamespaceRecord* n))return false;Copy((Byte*)_namespaces,(Byte*)n,(UInt64)_namespaceCapacity*(UInt64)sizeof(NamespaceRecord));KernelHeapAllocation old=_namespaceAllocation;_namespaceAllocation=a;_namespaces=n;_namespaceCapacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean GrowMounts(){if(_mode!=KernelStorageRegistryMode.Dynamic)return false;UInt32 next=KernelStorageMath.NextCapacity(_mountCapacity,_maximumMounts);if(next<=_mountCapacity||!AllocMounts(next,out KernelHeapAllocation a,out MountRecord* n))return false;Copy((Byte*)_mounts,(Byte*)n,(UInt64)_mountCapacity*(UInt64)sizeof(MountRecord));KernelHeapAllocation old=_mountAllocation;_mountAllocation=a;_mounts=n;_mountCapacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean GrowFiles(){if(_mode!=KernelStorageRegistryMode.Dynamic)return false;UInt32 next=KernelStorageMath.NextCapacity(_fileCapacity,_maximumOpenFiles);if(next<=_fileCapacity||!AllocFiles(next,out KernelHeapAllocation a,out FileRecord* n))return false;Copy((Byte*)_files,(Byte*)n,(UInt64)_fileCapacity*(UInt64)sizeof(FileRecord));KernelHeapAllocation old=_fileAllocation;_fileAllocation=a;_files=n;_fileCapacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean AllocProviders(UInt32 n,out KernelHeapAllocation a,out ProviderRecord* p){a=default;p=null;if(!KernelHeap.TryAllocate((UInt64)n*(UInt64)sizeof(ProviderRecord),64,true,out a))return false;p=(ProviderRecord*)(nuint)a.Address;return true;}
    private static Boolean AllocNamespaces(UInt32 n,out KernelHeapAllocation a,out NamespaceRecord* p){a=default;p=null;if(!KernelHeap.TryAllocate((UInt64)n*(UInt64)sizeof(NamespaceRecord),64,true,out a))return false;p=(NamespaceRecord*)(nuint)a.Address;return true;}
    private static Boolean AllocMounts(UInt32 n,out KernelHeapAllocation a,out MountRecord* p){a=default;p=null;if(!KernelHeap.TryAllocate((UInt64)n*(UInt64)sizeof(MountRecord),64,true,out a))return false;p=(MountRecord*)(nuint)a.Address;return true;}
    private static Boolean AllocFiles(UInt32 n,out KernelHeapAllocation a,out FileRecord* p){a=default;p=null;if(!KernelHeap.TryAllocate((UInt64)n*(UInt64)sizeof(FileRecord),64,true,out a))return false;p=(FileRecord*)(nuint)a.Address;return true;}
    private static void Clear(Byte* p,Int32 n){for(Int32 i=0;i<n;i++)p[i]=0;}
    private static void Copy(Byte* s,Byte* d,UInt64 n){for(UInt64 i=0;i<n;i++)d[i]=s[i];}
}
