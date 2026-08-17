using System;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Heap;

namespace NovaOryn.Kernel.Graphics;

/// <summary>Owns generic framebuffer targets independently of the firmware or graphics driver that created them.</summary>
public static unsafe class KernelGraphics
{
    private struct DisplayRecord { internal Byte Used,Kind,PixelFormat,CanSetMode,Primary; internal UInt32 Device,Width,Height,Pitch; internal UInt64 Physical,Virtual,Bytes,Present,SetMode; }
    private static DisplayRecord* _records; private static KernelHeapAllocation _allocation; private static UInt32 _capacity,_count,_firmwareCount,_simpleCount,_virtioCount,_primary; private static Boolean _initialized;

    /// <summary>Initializes the heap-backed graphics display registry.</summary>
    public static Boolean Initialize()
    {if(_initialized)return true;if(!KernelHeap.IsInitialized()||!Allocate(8U,out _allocation,out _records))return false;_capacity=8U;_initialized=true;return true;}
    /// <summary>Gets whether the generic graphics subsystem is initialized.</summary>
    public static Boolean IsInitialized()=>_initialized;
    /// <summary>Gets display counts and the current primary target.</summary>
    public static KernelGraphicsCapabilities GetCapabilities()=>new(_initialized,_count,_firmwareCount,_simpleCount,_virtioCount,new KernelGraphicsDisplayHandle(_primary));

    /// <summary>Registers a CPU-visible framebuffer supplied by firmware, a simple framebuffer source, or a graphics driver.</summary>
    public static Boolean RegisterDisplay(KernelDeviceHandle device,KernelGraphicsTargetKind kind,KernelGraphicsFramebuffer framebuffer,KernelGraphicsCallbacks callbacks,Boolean canSetMode,Boolean makePrimary,out KernelGraphicsDisplayHandle handle)
    {
        handle=default;if(!_initialized||kind==KernelGraphicsTargetKind.Unknown||!framebuffer.IsValid||callbacks.Present==null||(canSetMode&&callbacks.SetMode==null))return false;
        Int32 slot=Free();if(slot<0){if(!Grow())return false;slot=Free();if(slot<0)return false;}DisplayRecord* r=_records+slot;r->Used=1;r->Kind=(Byte)kind;r->PixelFormat=(Byte)framebuffer.Mode.PixelFormat;r->CanSetMode=canSetMode?(Byte)1:(Byte)0;r->Device=device.Value;r->Width=framebuffer.Mode.Width;r->Height=framebuffer.Mode.Height;r->Pitch=framebuffer.Mode.PixelsPerScanLine;r->Physical=framebuffer.PhysicalAddress;r->Virtual=framebuffer.VirtualAddress;r->Bytes=framebuffer.ByteLength;r->Present=(UInt64)(void*)callbacks.Present;r->SetMode=(UInt64)(void*)callbacks.SetMode;handle=new KernelGraphicsDisplayHandle((UInt32)slot+1U);_count++;if(kind==KernelGraphicsTargetKind.FirmwareFramebuffer)_firmwareCount++;else if(kind==KernelGraphicsTargetKind.SimpleFramebuffer)_simpleCount++;else if(kind==KernelGraphicsTargetKind.VirtioGpu)_virtioCount++;if(_primary==0U||makePrimary){if(_primary!=0U&&TryRecord(new KernelGraphicsDisplayHandle(_primary),out DisplayRecord* old))old->Primary=0;r->Primary=1;_primary=handle.Value;}return true;
    }

    /// <summary>Updates framebuffer metadata after a driver-owned mode change.</summary>
    public static Boolean UpdateFramebuffer(KernelGraphicsDisplayHandle handle,KernelGraphicsFramebuffer framebuffer)
    {if(!framebuffer.IsValid||!TryRecord(handle,out DisplayRecord* r))return false;r->PixelFormat=(Byte)framebuffer.Mode.PixelFormat;r->Width=framebuffer.Mode.Width;r->Height=framebuffer.Mode.Height;r->Pitch=framebuffer.Mode.PixelsPerScanLine;r->Physical=framebuffer.PhysicalAddress;r->Virtual=framebuffer.VirtualAddress;r->Bytes=framebuffer.ByteLength;return true;}
    /// <summary>Gets one registered display by zero-based enumeration index.</summary>
    public static Boolean TryGetDisplay(UInt32 index,out KernelGraphicsDisplayInfo info)
    {info=default;if(!_initialized||index>=_count)return false;UInt32 found=0;for(UInt32 i=0;i<_capacity;i++){DisplayRecord* r=_records+i;if(r->Used==0)continue;if(found++==index){info=Info(i,r);return true;}}return false;}
    /// <summary>Gets display information for an opaque handle.</summary>
    public static Boolean TryGetDisplay(KernelGraphicsDisplayHandle handle,out KernelGraphicsDisplayInfo info)
    {info=default;if(!TryRecord(handle,out DisplayRecord* r))return false;info=Info(handle.Value-1U,r);return true;}
    /// <summary>Gets the primary graphics target.</summary>
    public static Boolean TryGetPrimaryDisplay(out KernelGraphicsDisplayInfo info)=>TryGetDisplay(new KernelGraphicsDisplayHandle(_primary),out info);
    /// <summary>Requests a driver-owned display mode change.</summary>
    public static Boolean SetMode(KernelGraphicsDisplayHandle handle,KernelGraphicsMode mode)
    {if(!mode.IsValid||!TryRecord(handle,out DisplayRecord* r)||r->CanSetMode==0||r->SetMode==0)return false;delegate*<KernelGraphicsDisplayHandle,KernelGraphicsMode,Boolean> callback=(delegate*<KernelGraphicsDisplayHandle,KernelGraphicsMode,Boolean>)(void*)r->SetMode;return callback(handle,mode);}
    /// <summary>Presents a modified rectangle from the current framebuffer to the display scan-out.</summary>
    public static Boolean Present(KernelGraphicsDisplayHandle handle,UInt32 x,UInt32 y,UInt32 width,UInt32 height)
    {if(!TryRecord(handle,out DisplayRecord* r)||r->Present==0||width==0U||height==0U||x>=r->Width||y>=r->Height||width>r->Width-x||height>r->Height-y)return false;delegate*<KernelGraphicsDisplayHandle,UInt32,UInt32,UInt32,UInt32,Boolean> callback=(delegate*<KernelGraphicsDisplayHandle,UInt32,UInt32,UInt32,UInt32,Boolean>)(void*)r->Present;return callback(handle,x,y,width,height);}
    /// <summary>Selects any registered display as the generic primary graphics target.</summary>
    public static Boolean SetPrimaryDisplay(KernelGraphicsDisplayHandle handle)
    {if(!TryRecord(handle,out DisplayRecord* r))return false;if(_primary!=0U&&TryRecord(new KernelGraphicsDisplayHandle(_primary),out DisplayRecord* old))old->Primary=0;r->Primary=1;_primary=handle.Value;return true;}

    private static KernelGraphicsDisplayInfo Info(UInt32 index,DisplayRecord* r){KernelGraphicsMode mode=new(r->Width,r->Height,r->Pitch,(KernelGraphicsPixelFormat)r->PixelFormat);KernelGraphicsFramebuffer fb=new(r->Physical,r->Virtual,r->Bytes,mode);return new(new KernelGraphicsDisplayHandle(index+1U),new KernelDeviceHandle(r->Device),(KernelGraphicsTargetKind)r->Kind,fb,r->CanSetMode!=0,r->Primary!=0);}
    private static Boolean TryRecord(KernelGraphicsDisplayHandle h,out DisplayRecord* r){r=null;if(!_initialized||h.Value==0U||h.Value>_capacity)return false;DisplayRecord* p=_records+(h.Value-1U);if(p->Used==0)return false;r=p;return true;}
    private static Int32 Free(){for(Int32 i=0;i<(Int32)_capacity;i++)if((_records+i)->Used==0)return i;return -1;}
    private static Boolean Allocate(UInt32 capacity,out KernelHeapAllocation allocation,out DisplayRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DisplayRecord),64U,true,out allocation))return false;pointer=(DisplayRecord*)(nuint)allocation.Address;return true;}
    private static Boolean Grow(){UInt32 next=_capacity>=0x40000000U?UInt32.MaxValue:_capacity*2U;if(next<=_capacity||next>Int32.MaxValue||!Allocate(next,out KernelHeapAllocation fresh,out DisplayRecord* p))return false;Copy((Byte*)_records,(Byte*)p,(UInt64)_capacity*(UInt64)sizeof(DisplayRecord));KernelHeapAllocation old=_allocation;_allocation=fresh;_records=p;_capacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}
}
