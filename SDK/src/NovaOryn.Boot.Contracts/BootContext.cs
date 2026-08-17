using NovaOryn.Primitives;

namespace NovaOryn.Boot.Contracts;

public enum BootProtocol
{
    Unknown = 0,
    Uefi = 1,
    Limine = 2,
    Multiboot2 = 3
}

public enum FramebufferPixelFormat
{
    RedGreenBlueReserved8BitPerColor = 0,
    BlueGreenRedReserved8BitPerColor = 1,
    BitMask = 2,
    BltOnly = 3
}

public readonly struct PixelBitMask
{
    public PixelBitMask(uint red, uint green, uint blue, uint reserved)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Reserved = reserved;
    }

    public uint Red { get; }
    public uint Green { get; }
    public uint Blue { get; }
    public uint Reserved { get; }

    public bool IsDirectColor()
    {
        if (Red == 0 || Green == 0 || Blue == 0) return false;
        if ((Red & Green) != 0 || (Red & Blue) != 0 || (Green & Blue) != 0) return false;
        if (Reserved != 0 && ((Reserved & Red) != 0 || (Reserved & Green) != 0 || (Reserved & Blue) != 0)) return false;
        return IsContiguous(Red) && IsContiguous(Green) && IsContiguous(Blue);
    }

    private static bool IsContiguous(uint mask)
    {
        while ((mask & 1U) == 0U) mask >>= 1;
        while ((mask & 1U) != 0U) mask >>= 1;
        return mask == 0U;
    }
}

public readonly struct Framebuffer
{
    public Framebuffer(
        PhysicalAddress address,
        ulong sizeInBytes,
        uint width,
        uint height,
        uint pixelsPerScanLine,
        FramebufferPixelFormat pixelFormat,
        PixelBitMask pixelMask)
    {
        Address = address;
        SizeInBytes = sizeInBytes;
        Width = width;
        Height = height;
        PixelsPerScanLine = pixelsPerScanLine;
        PixelFormat = pixelFormat;
        PixelMask = pixelMask;
    }

    public PhysicalAddress Address { get; }
    public ulong SizeInBytes { get; }
    public uint Width { get; }
    public uint Height { get; }
    public uint PixelsPerScanLine { get; }
    public FramebufferPixelFormat PixelFormat { get; }
    public PixelBitMask PixelMask { get; }

    public bool IsAvailable()
    {
        if (Address.Value == 0 || SizeInBytes == 0 || Width == 0 || Height == 0) return false;
        if (PixelsPerScanLine < Width || PixelFormat > FramebufferPixelFormat.BitMask) return false;
        ulong bytesPerScanLine = checked((ulong)PixelsPerScanLine * 4UL);
        if ((ulong)Height > SizeInBytes / bytesPerScanLine) return false;
        return PixelFormat != FramebufferPixelFormat.BitMask || PixelMask.IsDirectColor();
    }
}

public readonly struct BootContext
{
    public BootContext(
        BootProtocol protocol,
        Framebuffer framebuffer,
        PhysicalAddress memoryMapAddress,
        ulong memoryMapLength)
        : this(protocol, framebuffer, memoryMapAddress, memoryMapLength, 0, 0, 0, false)
    {
    }

    /// <summary>Creates a boot context containing the accepted final firmware memory-map metadata.</summary>
    /// <nova.when>Use after firmware has accepted the map key and boot services have exited.</nova.when>
    /// <nova.depends>The map storage must remain valid for the lifetime of the boot context.</nova.depends>
    /// <returns>A boot context that identifies the retained final map and its descriptor format.</returns>
    /// <example><code>BootContext context = new(BootProtocol.Uefi, framebuffer, mapAddress, mapLength, mapKey, descriptorSize, descriptorVersion, true);</code></example>
    /// <param name="protocol">Firmware or loader protocol used to enter the kernel.</param>
    /// <param name="framebuffer">Framebuffer information captured before firmware services ended.</param>
    /// <param name="memoryMapAddress">Physical address of the retained final map.</param>
    /// <param name="memoryMapLength">Length of the retained map in bytes.</param>
    /// <param name="memoryMapKey">Firmware key accepted by ExitBootServices.</param>
    /// <param name="memoryDescriptorSize">Size of each firmware descriptor.</param>
    /// <param name="memoryDescriptorVersion">Firmware descriptor format version.</param>
    /// <param name="isFinalMemoryMap">Whether this is the map accepted immediately before ExitBootServices.</param>
    public BootContext(
        BootProtocol protocol,
        Framebuffer framebuffer,
        PhysicalAddress memoryMapAddress,
        ulong memoryMapLength,
        ulong memoryMapKey,
        ulong memoryDescriptorSize,
        uint memoryDescriptorVersion,
        bool isFinalMemoryMap)
        : this(protocol, framebuffer, memoryMapAddress, memoryMapLength, memoryMapKey, memoryDescriptorSize, memoryDescriptorVersion, isFinalMemoryMap, new PhysicalAddress(0), new PhysicalAddress(0))
    {
    }

    /// <summary>Creates a boot context with final firmware memory-map metadata and a captured ACPI root pointer.</summary>
    /// <param name="protocol">Firmware or loader protocol used to enter the kernel.</param>
    /// <param name="framebuffer">Framebuffer information captured before firmware services ended.</param>
    /// <param name="memoryMapAddress">Physical address of the retained final map.</param>
    /// <param name="memoryMapLength">Length of the retained map in bytes.</param>
    /// <param name="memoryMapKey">Firmware key accepted by ExitBootServices.</param>
    /// <param name="memoryDescriptorSize">Size of each firmware descriptor.</param>
    /// <param name="memoryDescriptorVersion">Firmware descriptor format version.</param>
    /// <param name="isFinalMemoryMap">Whether this is the map accepted immediately before ExitBootServices.</param>
    /// <param name="acpiRootPointerAddress">Physical address of the ACPI RSDP, or zero when unavailable.</param>
    /// <returns>A boot context containing memory and hardware-discovery hand-off data.</returns>
    public BootContext(
        BootProtocol protocol,
        Framebuffer framebuffer,
        PhysicalAddress memoryMapAddress,
        ulong memoryMapLength,
        ulong memoryMapKey,
        ulong memoryDescriptorSize,
        uint memoryDescriptorVersion,
        bool isFinalMemoryMap,
        PhysicalAddress acpiRootPointerAddress)
        : this(protocol, framebuffer, memoryMapAddress, memoryMapLength, memoryMapKey, memoryDescriptorSize, memoryDescriptorVersion, isFinalMemoryMap, acpiRootPointerAddress, new PhysicalAddress(0))
    {
    }

    /// <summary>Creates a boot context with ACPI and x86 application-processor startup hand-off addresses.</summary>
    /// <param name="protocol">Firmware or loader protocol used to enter the kernel.</param>
    /// <param name="framebuffer">Framebuffer information captured before firmware services ended.</param>
    /// <param name="memoryMapAddress">Physical address of the retained final map.</param>
    /// <param name="memoryMapLength">Length of the retained map in bytes.</param>
    /// <param name="memoryMapKey">Firmware key accepted by ExitBootServices.</param>
    /// <param name="memoryDescriptorSize">Size of each firmware descriptor.</param>
    /// <param name="memoryDescriptorVersion">Firmware descriptor format version.</param>
    /// <param name="isFinalMemoryMap">Whether this is the accepted final firmware map.</param>
    /// <param name="acpiRootPointerAddress">Physical address of the ACPI RSDP.</param>
    /// <param name="applicationProcessorTrampolineAddress">Physical address of a reserved SIPI page below 1 MiB, or zero when unavailable.</param>
    /// <returns>A boot context with memory, ACPI, and SMP bootstrap hand-off data.</returns>
    public BootContext(
        BootProtocol protocol,
        Framebuffer framebuffer,
        PhysicalAddress memoryMapAddress,
        ulong memoryMapLength,
        ulong memoryMapKey,
        ulong memoryDescriptorSize,
        uint memoryDescriptorVersion,
        bool isFinalMemoryMap,
        PhysicalAddress acpiRootPointerAddress,
        PhysicalAddress applicationProcessorTrampolineAddress)
    {
        Protocol = protocol;
        Framebuffer = framebuffer;
        MemoryMapAddress = memoryMapAddress;
        MemoryMapLength = memoryMapLength;
        MemoryMapKey = memoryMapKey;
        MemoryDescriptorSize = memoryDescriptorSize;
        MemoryDescriptorVersion = memoryDescriptorVersion;
        IsFinalMemoryMap = isFinalMemoryMap;
        AcpiRootPointerAddress = acpiRootPointerAddress;
        ApplicationProcessorTrampolineAddress = applicationProcessorTrampolineAddress;
    }

    public BootProtocol Protocol { get; }
    public Framebuffer Framebuffer { get; }
    public PhysicalAddress MemoryMapAddress { get; }
    public ulong MemoryMapLength { get; }
    /// <summary>Gets the firmware map key accepted by ExitBootServices.</summary>
    /// <nova.when>Use for diagnostics that prove the retained map was the accepted final map.</nova.when>
    public ulong MemoryMapKey { get; }
    /// <summary>Gets the size in bytes of each retained firmware memory descriptor.</summary>
    /// <nova.when>Use when enumerating the variable-sized UEFI descriptor array.</nova.when>
    public ulong MemoryDescriptorSize { get; }
    /// <summary>Gets the retained firmware memory-descriptor format version.</summary>
    /// <nova.when>Use when validating that a decoder supports the firmware descriptor format.</nova.when>
    public uint MemoryDescriptorVersion { get; }
    /// <summary>Gets whether the retained map was accepted immediately before ExitBootServices.</summary>
    /// <nova.when>Require this before constructing a physical-memory allocator.</nova.when>
    public bool IsFinalMemoryMap { get; }
    /// <summary>Gets the ACPI Root System Description Pointer captured by the boot environment.</summary>
    /// <nova.when>Use as the ACPI discovery root instead of scanning legacy BIOS address ranges.</nova.when>
    public PhysicalAddress AcpiRootPointerAddress { get; }
    /// <summary>Gets a reserved x86 SIPI target page below 1 MiB when the boot environment supplied one.</summary>
    public PhysicalAddress ApplicationProcessorTrampolineAddress { get; }

    public bool TryGetFramebuffer(out Framebuffer framebuffer)
    {
        framebuffer = Framebuffer;
        return framebuffer.IsAvailable();
    }
}
