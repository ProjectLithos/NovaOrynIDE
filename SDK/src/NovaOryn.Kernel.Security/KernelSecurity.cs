using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Protection;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.VirtualMemory;

namespace NovaOryn.Kernel.Security;

/// <summary>Central process-isolation, user-pointer, W^X, syscall-policy and capability/handle authority.</summary>
public static unsafe class KernelSecurity
{
    private const UInt32 MaximumProcesses=16U, MaximumCapabilities=256U, MaximumSyscallService=4095U;
    private const UInt64 Present=1UL, Writable=2UL, User=4UL, Large=128UL, NoExecute=1UL<<63, AddressMask=0x000FFFFFFFFFF000UL;
    private struct SecurityState
    {
        internal fixed UInt64 ProcessIds[(Int32)MaximumProcesses],Roots[(Int32)MaximumProcesses],GuardBases[(Int32)MaximumProcesses],GuardBytes[(Int32)MaximumProcesses];
        internal fixed UInt32 SyscallMasks[(Int32)MaximumProcesses];
        internal fixed UInt64 CurrentProcessIds[256];
        internal fixed UInt64 CapabilityOwners[(Int32)MaximumCapabilities],CapabilityObjects[(Int32)MaximumCapabilities],CapabilityRights[(Int32)MaximumCapabilities];
        internal fixed UInt32 CapabilityGenerations[(Int32)MaximumCapabilities],CapabilityInUse[(Int32)MaximumCapabilities];
    }
#pragma warning disable CS0169
    private static SecurityState _state;
#pragma warning restore CS0169
    private static Boolean _initialized; private static UInt32 _registered;

    public static Boolean Initialize()
    {
        if(_initialized)return true;
        if(!KernelProtection.IsInitialized()||!KernelVirtualMemory.IsInitialized())return false;
        KernelProtectionCapabilities p=KernelProtection.GetCapabilities(); if(!p.ExecuteDisableEnabled)return false;
        _initialized=true; return true;
    }
    public static Boolean IsInitialized()=>_initialized;
    public static KernelSecurityCapabilities GetCapabilities()=>new(KernelProtection.GetCapabilities().ExecuteDisableEnabled,true,true,_registered,MaximumCapabilities);

    /// <summary>Registers one private lower-half process address space and its intentionally unmapped guard range.</summary>
    public static Boolean RegisterProcessAddressSpace(UInt64 processId,UInt64 rootPhysicalAddress,UInt64 guardBase,UInt64 guardBytes)
    {
        if(!_initialized||processId==0UL||rootPhysicalAddress==0UL)return false; Int32 slot=FindProcess(processId); if(slot<0)slot=FindFreeProcess(); if(slot<0)return false;
        fixed(UInt64* ids=_state.ProcessIds,roots=_state.Roots,guards=_state.GuardBases,bytes=_state.GuardBytes) fixed(UInt32* masks=_state.SyscallMasks)
        { if(ids[slot]==0UL)_registered++; ids[slot]=processId; roots[slot]=rootPhysicalAddress; guards[slot]=guardBase; bytes[slot]=guardBytes; masks[slot]=0x0EU; }
        return guardBytes==0UL || IsRangeUnmapped(rootPhysicalAddress,guardBase,guardBytes);
    }
    public static Boolean UnregisterProcess(UInt64 processId)
    {
        Int32 slot=FindProcess(processId); if(slot<0)return false; RevokeProcessCapabilities(processId);
        fixed(UInt64* ids=_state.ProcessIds,roots=_state.Roots,guards=_state.GuardBases,bytes=_state.GuardBytes) fixed(UInt32* masks=_state.SyscallMasks)
        { ids[slot]=roots[slot]=guards[slot]=bytes[slot]=0UL; masks[slot]=0U; }
        if(_registered!=0U)_registered--; fixed(UInt64* current=_state.CurrentProcessIds)for(Int32 i=0;i<256;i++)if(current[i]==processId)current[i]=0UL; return true;
    }
    /// <summary>Updates the process identity used by protected syscall entry/exit.</summary>
    public static Boolean SetCurrentProcess(UInt64 processId){if(processId!=0UL&&FindProcess(processId)<0)return false;UInt32 cpu=0U;if(KernelSmp.IsInitialized()&&!KernelSmp.TryGetCurrentProcessorIndex(out cpu))return false;if(cpu>=256U)return false;fixed(UInt64* current=_state.CurrentProcessIds)current[cpu]=processId;return true;}
    public static Boolean TryGetCurrentProcess(out UInt64 processId){processId=0UL;if(!_initialized)return false;UInt32 cpu=0U;if(KernelSmp.IsInitialized()&&!KernelSmp.TryGetCurrentProcessorIndex(out cpu))return false;if(cpu>=256U)return false;fixed(UInt64* current=_state.CurrentProcessIds)processId=current[cpu];return processId!=0UL;}

    public static Boolean TryGetAddressSpace(UInt64 processId,out KernelAddressSpaceSecurityInfo info)
    {
        info=default; if(processId==0UL){UInt64 kr=KernelVirtualMemory.GetRootPhysicalAddress();if(kr==0UL)return false;info=new(0UL,kr,KernelAddressSpaceDomain.Kernel,0UL,0UL);return true;}
        Int32 slot=FindProcess(processId);if(slot<0)return false;fixed(UInt64* roots=_state.Roots,guards=_state.GuardBases,bytes=_state.GuardBytes){info=new(processId,roots[slot],KernelAddressSpaceDomain.User,guards[slot],bytes[slot]);return true;}
    }

    /// <summary>Validates every page of a user range against that process's own page-table root.</summary>
    public static Boolean TryValidateUserPointer(UInt64 processId,UInt64 address,UInt64 length,KernelUserMemoryAccess access)
    {
        if(!_initialized||!KernelProtectionMath.IsUserRange(address,length)||processId==0UL)return false; Int32 slot=FindProcess(processId);if(slot<0)return false;
        fixed(UInt64* roots=_state.Roots,guards=_state.GuardBases,guardBytes=_state.GuardBytes)
        {
            if(RangesOverlap(address,length,guards[slot],guardBytes[slot]))return false; UInt64 current=address,remaining=length;
            while(remaining!=0UL){if(!TryTranslate(roots[slot],current,out UInt64 _,out UInt64 flags,out UInt64 pageSize))return false;if((flags&User)==0UL)return false;if((access&KernelUserMemoryAccess.Write)!=0 && (flags&Writable)==0UL)return false;if((access&KernelUserMemoryAccess.Execute)!=0 && (flags&NoExecute)!=0UL)return false;UInt64 rem=pageSize-(current&(pageSize-1UL));UInt64 step=remaining<rem?remaining:rem;current+=step;remaining-=step;}
        }
        return true;
    }

    /// <summary>Applies page permissions to a private process range. W+X is rejected unconditionally.</summary>
    public static Boolean TryProtectUserRange(UInt64 processId,UInt64 address,UInt64 length,KernelVirtualMemoryProtection protection)
    {
        if(!_initialized||!IsUserProtectionAllowed(protection)||!KernelProtectionMath.IsUserRange(address,length)||(address&4095UL)!=0UL||(length&4095UL)!=0UL)return false;Int32 slot=FindProcess(processId);if(slot<0)return false;
        fixed(UInt64* roots=_state.Roots,guards=_state.GuardBases,guardBytes=_state.GuardBytes){if(RangesOverlap(address,length,guards[slot],guardBytes[slot]))return false;for(UInt64 p=0;p<length;p+=4096UL)if(!TryProtectLeaf(roots[slot],address+p,protection))return false;}
        return true;
    }
    public static Boolean IsUserProtectionAllowed(KernelVirtualMemoryProtection protection)
    { if((protection&KernelVirtualMemoryProtection.User)==0)return false; return !((protection&KernelVirtualMemoryProtection.Write)!=0&&(protection&KernelVirtualMemoryProtection.Execute)!=0); }
    public static Boolean TryValidateExecutableRange(UInt64 processId,UInt64 address,UInt64 length)=>TryValidateUserPointer(processId,address,length,KernelUserMemoryAccess.Read|KernelUserMemoryAccess.Execute);

    /// <summary>Enables/disables one syscall ABI for a process. ABI values are NovaOryn=1, Linux=2, NT=3.</summary>
    public static Boolean TrySetSyscallAbiPolicy(UInt64 processId,UInt32 abi,Boolean allowed)
    {Int32 slot=FindProcess(processId);if(slot<0||abi<1U||abi>3U)return false;fixed(UInt32* masks=_state.SyscallMasks){UInt32 bit=1U<<(Int32)abi;if(allowed)masks[slot]|=bit;else masks[slot]&=~bit;}return true;}
    public static Boolean TryValidateSyscall(UInt64 processId,UInt32 abi,UInt32 service)
    {Int32 slot=FindProcess(processId);if(slot<0||abi<1U||abi>3U||service>MaximumSyscallService)return false;fixed(UInt32* masks=_state.SyscallMasks)return (masks[slot]&(1U<<(Int32)abi))!=0U;}
    public static Boolean TryValidateCurrentSyscall(UInt32 abi,UInt32 service)=>TryGetCurrentProcess(out UInt64 processId)&&TryValidateSyscall(processId,abi,service);

    public static Boolean TryCreateCapability(UInt64 processId,UInt64 objectId,KernelCapabilityRights rights,out KernelCapabilityHandle handle)
    {
        handle=default;if(!_initialized||FindProcess(processId)<0||objectId==0UL||rights==KernelCapabilityRights.None)return false;
        fixed(UInt32* use=_state.CapabilityInUse,gen=_state.CapabilityGenerations) fixed(UInt64* owners=_state.CapabilityOwners,objects=_state.CapabilityObjects,r=_state.CapabilityRights)
        for(UInt32 i=0;i<MaximumCapabilities;i++)if(use[i]==0U){UInt32 g=gen[i]+1U;if(g==0U)g=1U;gen[i]=g;use[i]=1U;owners[i]=processId;objects[i]=objectId;r[i]=(UInt64)rights;handle=new(((UInt64)g<<32)|(i+1U));return true;}
        return false;
    }
    public static Boolean TryResolveCapability(UInt64 processId,KernelCapabilityHandle handle,KernelCapabilityRights requiredRights,out UInt64 objectId)
    {
        objectId=0UL;if(!Decode(handle,out UInt32 slot,out UInt32 generation))return false;
        fixed(UInt32* use=_state.CapabilityInUse,gen=_state.CapabilityGenerations) fixed(UInt64* owners=_state.CapabilityOwners,objects=_state.CapabilityObjects,r=_state.CapabilityRights)
        {if(use[slot]==0U||gen[slot]!=generation||owners[slot]!=processId||((KernelCapabilityRights)r[slot]&requiredRights)!=requiredRights)return false;objectId=objects[slot];return true;}
    }
    public static Boolean TryCloseCapability(UInt64 processId,KernelCapabilityHandle handle)
    {if(!Decode(handle,out UInt32 slot,out UInt32 generation))return false;fixed(UInt32* use=_state.CapabilityInUse,gen=_state.CapabilityGenerations)fixed(UInt64* owners=_state.CapabilityOwners,objects=_state.CapabilityObjects,r=_state.CapabilityRights){if(use[slot]==0U||gen[slot]!=generation||owners[slot]!=processId)return false;use[slot]=0U;owners[slot]=objects[slot]=r[slot]=0UL;return true;}}
    public static Boolean TryDuplicateCapability(UInt64 processId,KernelCapabilityHandle source,KernelCapabilityRights rights,out KernelCapabilityHandle duplicate)
    {duplicate=default;if(!TryResolveCapability(processId,source,rights,out UInt64 objectId))return false;return TryCreateCapability(processId,objectId,rights,out duplicate);}

    private static Boolean Decode(KernelCapabilityHandle h,out UInt32 slot,out UInt32 generation){slot=0;generation=0;UInt32 encoded=(UInt32)(h.Value&0xFFFFFFFFUL);if(encoded==0U||encoded>MaximumCapabilities)return false;slot=encoded-1U;generation=(UInt32)(h.Value>>32);return generation!=0U;}
    private static void RevokeProcessCapabilities(UInt64 processId){fixed(UInt32* use=_state.CapabilityInUse)fixed(UInt64* owners=_state.CapabilityOwners,objects=_state.CapabilityObjects,r=_state.CapabilityRights)for(UInt32 i=0;i<MaximumCapabilities;i++)if(use[i]!=0U&&owners[i]==processId){use[i]=0U;owners[i]=objects[i]=r[i]=0UL;}}
    private static Int32 FindProcess(UInt64 id){fixed(UInt64* ids=_state.ProcessIds)for(Int32 i=0;i<(Int32)MaximumProcesses;i++)if(ids[i]==id)return i;return -1;}
    private static Int32 FindFreeProcess(){fixed(UInt64* ids=_state.ProcessIds)for(Int32 i=0;i<(Int32)MaximumProcesses;i++)if(ids[i]==0UL)return i;return -1;}
    private static Boolean RangesOverlap(UInt64 a,UInt64 al,UInt64 b,UInt64 bl){if(al==0UL||bl==0UL)return false;UInt64 ae=a+al-1UL,be=b+bl-1UL;return a<=be&&b<=ae;}
    private static Boolean IsRangeUnmapped(UInt64 root,UInt64 address,UInt64 length){if(length==0UL)return true;for(UInt64 p=0;p<length;p+=4096UL)if(TryTranslate(root,address+p,out UInt64 _,out UInt64 _,out UInt64 _))return false;return true;}
    private static UInt64* Table(UInt64 physical){return KernelAddressSpace.TryPhysicalToDirectMap(physical,out UInt64 v)?(UInt64*)(nuint)v:(UInt64*)(nuint)physical;}
    private static Boolean TryTranslate(UInt64 root,UInt64 va,out UInt64 pa,out UInt64 flags,out UInt64 pageSize)
    {
        pa=flags=pageSize=0UL;UInt64* pml4=Table(root);UInt64 e4=pml4[(va>>39)&511UL];if((e4&Present)==0)return false;UInt64* pdpt=Table(e4&AddressMask);UInt64 e3=pdpt[(va>>30)&511UL];if((e3&Present)==0)return false;if((e3&Large)!=0){pageSize=1UL<<30;flags=e3;pa=(e3&0x000FFFFFC0000000UL)|(va&(pageSize-1UL));return true;}UInt64* pd=Table(e3&AddressMask);UInt64 e2=pd[(va>>21)&511UL];if((e2&Present)==0)return false;if((e2&Large)!=0){pageSize=1UL<<21;flags=e2;pa=(e2&0x000FFFFFFFE00000UL)|(va&(pageSize-1UL));return true;}UInt64* pt=Table(e2&AddressMask);UInt64 e1=pt[(va>>12)&511UL];if((e1&Present)==0)return false;pageSize=4096UL;flags=e1;pa=(e1&AddressMask)|(va&4095UL);return true;
    }
    private static Boolean TryProtectLeaf(UInt64 root,UInt64 va,KernelVirtualMemoryProtection protection)
    {
        UInt64* pml4=Table(root);UInt64 e4=pml4[(va>>39)&511UL];if((e4&Present)==0)return false;UInt64* pdpt=Table(e4&AddressMask);UInt64 e3=pdpt[(va>>30)&511UL];if((e3&Present)==0||(e3&Large)!=0)return false;UInt64* pd=Table(e3&AddressMask);UInt64 e2=pd[(va>>21)&511UL];if((e2&Present)==0||(e2&Large)!=0)return false;UInt64* pt=Table(e2&AddressMask);UInt64* leaf=&pt[(va>>12)&511UL];if((*leaf&Present)==0)return false;UInt64 f=Present|User;if((protection&KernelVirtualMemoryProtection.Write)!=0)f|=Writable;if((protection&KernelVirtualMemoryProtection.Execute)==0)f|=NoExecute;*leaf=(*leaf&AddressMask)|f;return true;
    }
}
