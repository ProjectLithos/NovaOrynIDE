using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.VirtualMemory;

namespace NovaOryn.Kernel.Processes;

internal static unsafe class ProcessAddressSpace
{
    private const UInt64 Present=1UL, Writable=2UL, User=4UL, NoExecute=1UL<<63, AddressMask=0x000FFFFFFFFFF000UL;

    internal static Boolean TryCreate(KernelPhysicalAllocation* tables, UInt32 capacity, out UInt32 tableCount, out UInt64 root)
    {
        tableCount=0U; root=0UL;
        if(!AllocateTable(tables,capacity,ref tableCount,out KernelPhysicalAllocation allocation)) return false;
        root=allocation.StartAddress; UInt64* dst=Pointer(root); UInt64 kernelRoot=KernelVirtualMemory.GetRootPhysicalAddress(); if(kernelRoot==0UL)return false; UInt64* src=Pointer(kernelRoot);
        for(Int32 i=0;i<256;i++)dst[i]=0UL; for(Int32 i=256;i<512;i++)dst[i]=src[i]; return true;
    }

    internal static Boolean TryMap(UInt64 root, UInt64 virtualAddress, UInt64 physicalAddress, KernelVirtualMemoryProtection protection, KernelPhysicalAllocation* tables, UInt32 capacity, ref UInt32 tableCount)
    {
        if((virtualAddress&4095UL)!=0UL || (physicalAddress&4095UL)!=0UL || virtualAddress>=KernelAddressSpace.UserEndExclusive || ((protection&KernelVirtualMemoryProtection.Write)!=0 && (protection&KernelVirtualMemoryProtection.Execute)!=0)) return false;
        UInt64* pml4=Pointer(root); Int32 a=(Int32)((virtualAddress>>39)&511UL), b=(Int32)((virtualAddress>>30)&511UL), c=(Int32)((virtualAddress>>21)&511UL), d=(Int32)((virtualAddress>>12)&511UL);
        if(a>=256)return false; if(!Child(pml4,a,tables,capacity,ref tableCount,out UInt64* pdpt))return false; if(!Child(pdpt,b,tables,capacity,ref tableCount,out UInt64* pd))return false; if(!Child(pd,c,tables,capacity,ref tableCount,out UInt64* pt))return false;
        if((pt[d]&Present)!=0UL)return false; UInt64 flags=Present|User; if((protection&KernelVirtualMemoryProtection.Write)!=0)flags|=Writable; if((protection&KernelVirtualMemoryProtection.Execute)==0)flags|=NoExecute; pt[d]=(physicalAddress&AddressMask)|flags; return true;
    }

    internal static Boolean TryReleaseTables(KernelPhysicalAllocation* tables, UInt32 count)
    { Boolean ok=true; for(UInt32 i=count;i>0U;i--) if(!KernelPhysicalMemory.TryRelease(tables[i-1U]))ok=false; return ok; }

    private static Boolean Child(UInt64* table, Int32 index, KernelPhysicalAllocation* tables, UInt32 capacity, ref UInt32 count, out UInt64* child)
    {
        child=(UInt64*)0; UInt64 e=table[index]; if((e&Present)!=0UL){child=Pointer(e&AddressMask);return true;}
        if(!AllocateTable(tables,capacity,ref count,out KernelPhysicalAllocation a))return false; table[index]=(a.StartAddress&AddressMask)|Present|Writable|User; child=Pointer(a.StartAddress); return true;
    }

    private static Boolean AllocateTable(KernelPhysicalAllocation* tables, UInt32 capacity, ref UInt32 count, out KernelPhysicalAllocation allocation)
    {
        allocation=default; if(count>=capacity || !KernelPhysicalMemory.TryAllocate(1UL,1UL,out allocation))return false; tables[count++]=allocation; UInt64* p=Pointer(allocation.StartAddress); for(Int32 i=0;i<512;i++)p[i]=0UL; return true;
    }

    private static UInt64* Pointer(UInt64 physical)
    { return KernelAddressSpace.TryPhysicalToDirectMap(physical,out UInt64 v)?(UInt64*)(nuint)v:(UInt64*)(nuint)physical; }
}
