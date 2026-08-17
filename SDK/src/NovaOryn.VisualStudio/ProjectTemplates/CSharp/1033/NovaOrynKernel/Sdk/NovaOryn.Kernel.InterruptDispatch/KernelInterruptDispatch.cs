using System;
using System.Runtime;
using System.Runtime.InteropServices;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.Heap;

namespace NovaOryn.Kernel.InterruptDispatch;

/// <summary>Owns freestanding managed interrupt dispatch and vector registration for the generated kernel.</summary>
public static unsafe class KernelInterruptDispatch
{
    private const Byte FirstDynamicVector=0x40,LastDynamicVector=0xEF; private const UInt64 LocalApicEoi=0xB0UL;
    [StructLayout(LayoutKind.Sequential,Pack=8)] private struct NativeInterruptContext { internal UInt64 Vector,ErrorCode,Rip,Cs,Rflags,Rsp,Ss,Cr0,Cr2,Cr3,Cr4,ProcessorId,PrivilegeTransition,Rax,Rbx,Rcx,Rdx,Rsi,Rdi,Rbp,R8,R9,R10,R11,R12,R13,R14,R15; }
    private static UInt64* _callbacks; private static UInt64* _cookies; private static Byte* _allocated; private static KernelHeapAllocation _tables; private static Boolean _initialized; private static UInt64 _localApicBase;
    /// <summary>Installs the managed native interrupt dispatcher without exposing IDT mechanics to drivers.</summary>
    public static Boolean Initialize(){if(_initialized)return true;if(!KernelHeap.IsInitialized()||!KernelAcpi.TryGetLocalApicAddress(out _localApicBase)||_localApicBase==0UL)return false;if(!KernelHeap.TryAllocate(4352UL,64UL,true,out _tables))return false;_callbacks=(UInt64*)(nuint)_tables.Address;_cookies=(UInt64*)(nuint)(_tables.Address+2048UL);_allocated=(Byte*)(nuint)(_tables.Address+4096UL);if(!Native.InstallManagedInterruptDispatcher())return false;_initialized=true;return true;}
    /// <summary>Gets whether managed interrupt dispatch is installed.</summary>
    public static Boolean IsInitialized()=>_initialized;
    /// <summary>Allocates one normal device/timer vector.</summary>
    public static Byte AllocateVector(){if(!_initialized)return 0;for(Int32 i=FirstDynamicVector;i<=LastDynamicVector;i++)if(_allocated[i]==0){_allocated[i]=1;return (Byte)i;}return 0;}
    /// <summary>Releases an unused dynamically allocated vector.</summary>
    public static Boolean ReleaseVector(Byte vector){if(vector<FirstDynamicVector||vector>LastDynamicVector||_allocated[vector]==0||_callbacks[vector]!=0UL)return false;_allocated[vector]=0;return true;}
    /// <summary>Registers one interrupt callback. The callback receives vector and caller cookie.</summary>
    public static Boolean Register(Byte vector,delegate*<Byte,UInt64,Boolean> callback,UInt64 cookie){if(!_initialized||vector<32||callback==null||_callbacks[vector]!=0UL)return false;_callbacks[vector]=(UInt64)(void*)callback;_cookies[vector]=cookie;return true;}
    /// <summary>Removes one registered callback.</summary>
    public static Boolean Unregister(Byte vector){if(!_initialized||_callbacks[vector]==0UL)return false;_callbacks[vector]=0UL;_cookies[vector]=0UL;return true;}
    /// <summary>Enables maskable processor interrupts after dispatch sources are registered.</summary>
    public static Boolean Enable()=>_initialized&&Native.EnableInterrupts();
    /// <summary>Idles until the next hardware interrupt and then returns.</summary>
    public static Boolean Wait()=>_initialized&&Native.WaitForInterrupt();
    [RuntimeExport("NovaOrynManagedInterruptDispatch")]
    private static Int32 Dispatch(UInt64 contextAddress){if(contextAddress==0UL)return 3;NativeInterruptContext* c=(NativeInterruptContext*)(nuint)contextAddress;UInt64 raw=c->Vector;if(raw>255UL)return 3;Byte vector=(Byte)raw;UInt64 address=_callbacks[vector];Boolean handled=false;if(address!=0UL){delegate*<Byte,UInt64,Boolean> cb=(delegate*<Byte,UInt64,Boolean>)(void*)address;handled=cb(vector,_cookies[vector]);}if(vector>=32&&_localApicBase!=0UL)Native.WriteMmio32(_localApicBase+LocalApicEoi,0U);if(vector<32&&!handled)return 3;return 1;}
}
