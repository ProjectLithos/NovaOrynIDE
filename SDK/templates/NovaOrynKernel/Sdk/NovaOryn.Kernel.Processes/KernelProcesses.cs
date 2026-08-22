using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Storage;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Protection;
using NovaOryn.Kernel.SystemCalls;
using NovaOryn.Kernel.Security;
using NovaOryn.Kernel.VirtualMemory;
using NovaOryn.ApplicationFormat;

namespace NovaOryn.Kernel.Processes;

/// <summary>Creates isolated x64 user processes directly from validated in-memory executable images.</summary>
public static unsafe class KernelProcesses
{
    private const UInt32 MaximumProcesses=8U, MaximumTablesPerProcess=64U, MaximumImageAllocations=17U;
    private const UInt64 DefaultStackBytes=1048576UL, UserStackTop=0x00007FFFFFF00000UL;
    private struct ProcessRecord
    {
        internal UInt64 Id, Root, Entry, StackBase, StackTop, StackGuardBase; internal Int64 ExitCode; internal UInt32 State, Format, TableCount, AllocationCount;
        internal fixed UInt64 TableTokens[(Int32)MaximumTablesPerProcess], TableStarts[(Int32)MaximumTablesPerProcess], TablePages[(Int32)MaximumTablesPerProcess];
        internal fixed UInt64 AllocationTokens[(Int32)MaximumImageAllocations], AllocationStarts[(Int32)MaximumImageAllocations], AllocationPages[(Int32)MaximumImageAllocations];
    }
    private struct ProcessTable { internal fixed Byte Bytes[(Int32)MaximumProcesses*2184]; }
#pragma warning disable CS0169
    private static ProcessTable _table;
#pragma warning restore CS0169
    private static UInt64 _nextId=1UL; private static UInt64 _currentProcessId; private static UInt32 _active; private static Boolean _initialized;

    /// <summary>Initializes the process facility after paging, protection and system calls are ready.</summary>
    public static Boolean Initialize()
    { if(_initialized)return true; if(!KernelAddressSpace.IsInitialized()||!KernelProtection.IsInitialized()||!KernelSecurity.IsInitialized()||!KernelSystemCalls.IsInitialized())return false; _initialized=true; return true; }
    public static Boolean IsInitialized() => _initialized;
    public static UInt32 GetActiveProcessCount() => _active;
    /// <summary>Gets the process currently owning the calling execution context, or zero while executing a kernel-only thread.</summary>
    public static Boolean TryGetCurrentProcessId(out UInt64 processId) { processId=_currentProcessId; return _initialized && processId!=0UL; }
    public static KernelProcessCapabilities GetCapabilities() => new(MaximumProcesses,_active,ProcessExecutableMath.MaximumSegments,DefaultStackBytes,true,true);

    /// <summary>Validates an x64 ELF64 or PE32+ image, builds a private lower-half address space and creates its initial user stack.</summary>
    public static Boolean TryCreateFromImage(UInt64 imageAddress, UInt64 imageLength, out KernelProcessInfo process)
    {
        process=default; if(!_initialized||imageAddress==0UL||imageLength==0UL)return false; Byte* packageOrImage=(Byte*)(nuint)imageAddress;
        if(!NovaOrynApplicationLoader.TryResolveNativeImage(packageOrImage,imageLength,out Byte* image,out UInt64 nativeLength,out NovaOrynApplicationInfo application,out Boolean packaged))return false;
        if(!ProcessExecutableMath.TryInspect(image,nativeLength,out ProcessExecutableInfo executable))return false;
        if(packaged && application.EntryPointRva!=0UL && executable.EntryPoint<executable.ImageBase)return false;
        if(packaged && application.EntryPointRva!=0UL && executable.EntryPoint-executable.ImageBase!=application.EntryPointRva)return false;
        Int32 slot=FindFreeSlot(); if(slot<0)return false; ProcessRecord* r=Record(slot); Clear(r);
        KernelPhysicalAllocation* tables=stackalloc KernelPhysicalAllocation[(Int32)MaximumTablesPerProcess]; UInt32 tableCount;
        if(!ProcessAddressSpace.TryCreate(tables,MaximumTablesPerProcess,out tableCount,out UInt64 root))return false;
        if(!LoadSegments(image,nativeLength,executable,root,r,tables,ref tableCount) || !CreateStack(root,r,tables,ref tableCount)) { ReleaseTemporary(r,tables,tableCount); return false; }
        r->TableCount=tableCount; for(UInt32 i=0;i<tableCount;i++){r->TableTokens[(Int32)i]=tables[i].Token;r->TableStarts[(Int32)i]=tables[i].StartAddress;r->TablePages[(Int32)i]=tables[i].PageCount;}
        r->Id=_nextId++; r->Root=root; r->Entry=executable.EntryPoint; if(!KernelSecurity.RegisterProcessAddressSpace(r->Id,root,r->StackGuardBase,4096UL)){ReleaseTemporary(r,tables,tableCount);return false;} if(!KernelSecurity.TryValidateExecutableRange(r->Id,r->Entry,1UL)){KernelSecurity.UnregisterProcess(r->Id);ReleaseTemporary(r,tables,tableCount);return false;} r->State=(UInt32)KernelProcessState.Ready; r->Format=(UInt32)executable.Format; _active++;
        process=Snapshot(r); return true;
    }

    /// <summary>Loads an ELF64 or PE32+ executable through the mounted VFS and creates an isolated user process.</summary>
    public static Boolean TryCreateFromFile(KernelMountNamespaceHandle mountNamespace, String path, out KernelProcessInfo process)
    {
        process=default;if(!_initialized||!KernelStorage.IsInitialized()||path==null)return false;if(!KernelVfs.Open(mountNamespace,path,KernelFileAccess.Read,out KernelFileHandle file))return false;
        if(!KernelVfs.TryGetFileInfo(file,out KernelVfsFileInfo info)||info.Type!=KernelFileType.File||info.Length==0UL){KernelVfs.Close(file);return false;}
        if(!KernelHeap.TryAllocate(info.Length,16UL,false,out KernelHeapAllocation image)){KernelVfs.Close(file);return false;}Byte* buffer=(Byte*)(nuint)image.Address;UInt64 total=0UL;Boolean ok=true;
        while(total<info.Length){UInt64 remaining=info.Length-total;UInt32 request=remaining>1048576UL?1048576U:(UInt32)remaining;if(!KernelVfs.Read(file,buffer+total,request,out UInt32 read)||read==0U){ok=false;break;}total+=read;}
        if(!KernelVfs.Close(file))ok=false;if(ok&&total==info.Length)ok=TryCreateFromImage(image.Address,info.Length,out process);if(!KernelHeap.TryRelease(image))ok=false;return ok;
    }

    /// <summary>Gets one active process by stable process identifier.</summary>
    public static Boolean TryGetProcess(UInt64 processId, out KernelProcessInfo process)
    { process=default; ProcessRecord* r=Find(processId); if(r==null)return false; process=Snapshot(r); return true; }

    /// <summary>Releases a process that has not entered user mode, or marks a stopped process terminated.</summary>
    public static Boolean TryTerminate(UInt64 processId, Int64 exitCode)
    {
        ProcessRecord* r=Find(processId); if(r==null || r->State==(UInt32)KernelProcessState.Running)return false; KernelSecurity.UnregisterProcess(processId); Boolean ok=ReleaseOwned(r); r->ExitCode=exitCode; r->State=(UInt32)KernelProcessState.Terminated; if(_active!=0U)_active--; return ok;
    }

    /// <summary>Switches to the process page-table root and enters x64 ring 3 at its executable entry point.</summary>
    /// <remarks>Successful entry intentionally does not return through this call; user code returns to ring 0 through interrupts or SYSCALL.</remarks>
    public static Boolean TryStart(UInt64 processId, UInt64 argument)
    {
        ProcessRecord* r=Find(processId); if(r==null || r->State!=(UInt32)KernelProcessState.Ready)return false; UInt64 kernelRoot=KernelVirtualMemory.GetRootPhysicalAddress();
        if(!Native.WritePageTableRoot(r->Root))return false; r->State=(UInt32)KernelProcessState.Running; _currentProcessId=r->Id; if(!KernelSecurity.SetCurrentProcess(r->Id)){_currentProcessId=0UL;Native.WritePageTableRoot(kernelRoot);r->State=(UInt32)KernelProcessState.Faulted;return false;}
        if(Native.EnterUserMode(r->Entry,r->StackTop,argument))return true; _currentProcessId=0UL; KernelSecurity.SetCurrentProcess(0UL); Native.WritePageTableRoot(kernelRoot); r->State=(UInt32)KernelProcessState.Faulted; return false;
    }

    private static Boolean LoadSegments(Byte* image, UInt64 length, ProcessExecutableInfo executable, UInt64 root, ProcessRecord* r, KernelPhysicalAllocation* tables, ref UInt32 tableCount)
    {
        for(UInt32 s=0;s<executable.SegmentCount;s++)
        {
            if(!ProcessExecutableMath.TryGetSegment(image,length,executable,s,out ProcessImageSegment segment))return false; if((segment.Protection&ProcessSegmentProtection.Write)!=0 && (segment.Protection&ProcessSegmentProtection.Execute)!=0)return false; UInt64 basePage=ProcessExecutableMath.PageFloor(segment.VirtualAddress);
            if(segment.VirtualAddress>UInt64.MaxValue-segment.MemorySize || !ProcessExecutableMath.TryPageCeiling(segment.VirtualAddress+segment.MemorySize,out UInt64 endPage))return false;
            UInt64 pages=(endPage-basePage)/4096UL; if(r->AllocationCount>=MaximumImageAllocations || !KernelPhysicalMemory.TryAllocate(pages,1UL,out KernelPhysicalAllocation allocation))return false;
            StoreAllocation(r,allocation); if(!ZeroAndCopy(allocation,segment.VirtualAddress-basePage,image+segment.FileOffset,segment.FileSize))return false;
            for(UInt64 p=0;p<pages;p++) if(!ProcessAddressSpace.TryMap(root,basePage+p*4096UL,allocation.StartAddress+p*4096UL,ToVirtualProtection(segment.Protection),tables,MaximumTablesPerProcess,ref tableCount))return false;
        }
        return true;
    }

    private static Boolean CreateStack(UInt64 root, ProcessRecord* r, KernelPhysicalAllocation* tables, ref UInt32 tableCount)
    {
        UInt64 pages=DefaultStackBytes/4096UL; if(r->AllocationCount>=MaximumImageAllocations || !KernelPhysicalMemory.TryAllocate(pages,1UL,out KernelPhysicalAllocation allocation))return false; StoreAllocation(r,allocation); UInt64 stackBase=UserStackTop-DefaultStackBytes; UInt64 guardBase=stackBase-4096UL;
        if(!ZeroAndCopy(allocation,0UL,(Byte*)0,0UL))return false; KernelVirtualMemoryProtection protection=KernelVirtualMemoryProtection.Read|KernelVirtualMemoryProtection.Write|KernelVirtualMemoryProtection.User;
        for(UInt64 p=0;p<pages;p++) if(!ProcessAddressSpace.TryMap(root,stackBase+p*4096UL,allocation.StartAddress+p*4096UL,protection,tables,MaximumTablesPerProcess,ref tableCount))return false;
        r->StackBase=stackBase; r->StackTop=UserStackTop; r->StackGuardBase=guardBase; return KernelProtectionMath.IsValidUserStack(r->StackTop);
    }

    private static Boolean ZeroAndCopy(KernelPhysicalAllocation allocation, UInt64 destinationOffset, Byte* source, UInt64 count)
    {
        if(!KernelAddressSpace.TryPhysicalToDirectMap(allocation.StartAddress,out UInt64 mapped))return false; Byte* destination=(Byte*)(nuint)mapped; UInt64 bytes=allocation.PageCount*4096UL;
        for(UInt64 i=0;i<bytes;i++)destination[i]=0; if(destinationOffset>bytes || count>bytes-destinationOffset)return false; for(UInt64 i=0;i<count;i++)destination[destinationOffset+i]=source[i]; return true;
    }

    private static KernelVirtualMemoryProtection ToVirtualProtection(ProcessSegmentProtection protection)
    { KernelVirtualMemoryProtection result=KernelVirtualMemoryProtection.Read|KernelVirtualMemoryProtection.User; if((protection&ProcessSegmentProtection.Write)!=0)result|=KernelVirtualMemoryProtection.Write; if((protection&ProcessSegmentProtection.Execute)!=0)result|=KernelVirtualMemoryProtection.Execute; return result; }
    private static Boolean ReleaseOwned(ProcessRecord* r)
    {
        Boolean ok=true; for(UInt32 i=r->AllocationCount;i>0U;i--) if(!KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(r->AllocationTokens[(Int32)(i-1U)],r->AllocationStarts[(Int32)(i-1U)],r->AllocationPages[(Int32)(i-1U)])))ok=false;
        for(UInt32 i=r->TableCount;i>0U;i--) if(!KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(r->TableTokens[(Int32)(i-1U)],r->TableStarts[(Int32)(i-1U)],r->TablePages[(Int32)(i-1U)])))ok=false; return ok;
    }
    private static Boolean ReleaseTemporary(ProcessRecord* r, KernelPhysicalAllocation* tables, UInt32 tableCount)
    { Boolean ok=true; for(UInt32 i=r->AllocationCount;i>0U;i--)if(!KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(r->AllocationTokens[(Int32)(i-1U)],r->AllocationStarts[(Int32)(i-1U)],r->AllocationPages[(Int32)(i-1U)])))ok=false; if(!ProcessAddressSpace.TryReleaseTables(tables,tableCount))ok=false; Clear(r); return ok; }
    private static void StoreAllocation(ProcessRecord* r, KernelPhysicalAllocation a) { UInt32 i=r->AllocationCount++;r->AllocationTokens[(Int32)i]=a.Token;r->AllocationStarts[(Int32)i]=a.StartAddress;r->AllocationPages[(Int32)i]=a.PageCount; }
    private static KernelProcessInfo Snapshot(ProcessRecord* r)=>new(r->Id,(KernelProcessState)r->State,(ProcessExecutableFormat)r->Format,r->Root,r->Entry,r->StackBase,r->StackTop,r->StackGuardBase,r->ExitCode);
    private static ProcessRecord* Find(UInt64 id){for(Int32 i=0;i<(Int32)MaximumProcesses;i++){ProcessRecord* r=Record(i);if(r->Id==id&&r->State!=(UInt32)KernelProcessState.Unused&&r->State!=(UInt32)KernelProcessState.Terminated)return r;}return null;}
    private static Int32 FindFreeSlot(){for(Int32 i=0;i<(Int32)MaximumProcesses;i++){UInt32 s=Record(i)->State;if(s==(UInt32)KernelProcessState.Unused||s==(UInt32)KernelProcessState.Terminated)return i;}return -1;}
    private static ProcessRecord* Record(Int32 slot){fixed(Byte* b=_table.Bytes)return (ProcessRecord*)(b+(UInt64)slot*(UInt64)sizeof(ProcessRecord));}
    private static void Clear(ProcessRecord* r){Byte* p=(Byte*)r;for(Int32 i=0;i<sizeof(ProcessRecord);i++)p[i]=0;}
}
