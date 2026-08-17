using System;

namespace NovaOryn.Kernel.Console;

#pragma warning disable CS0649 // Populated by the native UEFI entry before managed execution.
internal struct NativeBootContext
{
    internal UInt64 Signature;
    internal UInt64 FramebufferAddress;
    internal UInt64 FramebufferSize;
    internal UInt32 Width;
    internal UInt32 Height;
    internal UInt32 PixelsPerScanLine;
    internal UInt32 PixelFormat;
    internal UInt32 RedMask;
    internal UInt32 GreenMask;
    internal UInt32 BlueMask;
    internal UInt32 ReservedMask;
    internal UInt64 FinalMemoryMapAddress;
    internal UInt64 FinalMemoryMapLength;
    internal UInt64 FinalMemoryMapKey;
    internal UInt64 FinalMemoryDescriptorSize;
    internal UInt32 FinalMemoryDescriptorVersion;
    internal UInt32 FinalMemoryMapCaptureAttempts;
    internal UInt64 ExitBootServicesStatus;
    internal UInt64 FinalMemoryMapFlag;
    internal UInt64 BootstrapPageTableWorkspaceAddress;
    internal UInt64 BootstrapPageTableWorkspacePages;
    internal UInt64 AcpiRootPointerAddress;
    internal UInt64 ApplicationProcessorTrampolineAddress;
    internal UInt64 ApplicationProcessorTrampolinePages;
}
#pragma warning restore CS0649

public readonly unsafe struct BootContext
{
    private readonly UInt64 _nativeAddress;

    public BootContext(UInt64 nativeAddress)
    {
        _nativeAddress = nativeAddress;
    }

    internal NativeBootContext* GetNativeContext()
    {
        return (NativeBootContext*)_nativeAddress;
    }

    public Boolean IsAvailable()
    {
        NativeBootContext* context = GetNativeContext();
        return context != null && context->Signature == 0x4E59524F41564F4EUL;
    }

    public UInt64 GetFramebufferAddress()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FramebufferAddress;
    }

    public UInt64 GetFramebufferSize()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FramebufferSize;
    }

    public UInt32 GetFramebufferWidth()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0U : context->Width;
    }

    public UInt32 GetFramebufferHeight()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0U : context->Height;
    }

    public UInt32 GetFramebufferPitchInPixels()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0U : context->PixelsPerScanLine;
    }

    public UInt32 GetFramebufferPixelFormat()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 3U : context->PixelFormat;
    }

    public Boolean HasFinalMemoryMap()
    {
        NativeBootContext* context = GetNativeContext();
        if (context == null || context->FinalMemoryMapFlag != 1UL || context->ExitBootServicesStatus != 0UL) return false;
        if (context->FinalMemoryMapAddress == 0UL || context->FinalMemoryMapLength == 0UL) return false;
        if (context->FinalMemoryDescriptorSize < 40UL || (context->FinalMemoryDescriptorSize & 7UL) != 0UL) return false;
        if (context->FinalMemoryMapLength % context->FinalMemoryDescriptorSize != 0UL) return false;
        return context->FinalMemoryMapAddress <= 0xFFFFFFFFFFFFFFFFUL - context->FinalMemoryMapLength;
    }

    public UInt64 GetFinalMemoryMapAddress()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FinalMemoryMapAddress;
    }

    public UInt64 GetFinalMemoryMapLength()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FinalMemoryMapLength;
    }

    public UInt64 GetFinalMemoryMapKey()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FinalMemoryMapKey;
    }

    public UInt64 GetFinalMemoryDescriptorSize()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->FinalMemoryDescriptorSize;
    }

    public UInt32 GetFinalMemoryDescriptorVersion()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0U : context->FinalMemoryDescriptorVersion;
    }

    public UInt32 GetFinalMemoryMapCaptureAttempts()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0U : context->FinalMemoryMapCaptureAttempts;
    }

    /// <summary>Gets the physical base of the UEFI-reserved bootstrap page-table workspace.</summary>
    public UInt64 GetBootstrapPageTableWorkspaceAddress()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->BootstrapPageTableWorkspaceAddress;
    }

    /// <summary>Gets the number of 4 KiB pages reserved before ExitBootServices for bootstrap page tables.</summary>
    public UInt64 GetBootstrapPageTableWorkspacePages()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->BootstrapPageTableWorkspacePages;
    }

    /// <summary>Gets the physical ACPI Root System Description Pointer captured from the UEFI configuration table.</summary>
    public UInt64 GetAcpiRootPointerAddress()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->AcpiRootPointerAddress;
    }

    /// <summary>Gets the UEFI-reserved physical SIPI trampoline page below 1 MiB.</summary>
    public UInt64 GetApplicationProcessorTrampolineAddress()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->ApplicationProcessorTrampolineAddress;
    }

    /// <summary>Gets the number of pages reserved for the application-processor startup trampoline.</summary>
    public UInt64 GetApplicationProcessorTrampolinePages()
    {
        NativeBootContext* context = GetNativeContext();
        return context == null ? 0UL : context->ApplicationProcessorTrampolinePages;
    }
}
