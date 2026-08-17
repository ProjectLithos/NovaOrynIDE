using System.Buffers.Binary;
using System.Text;

internal static class EfiDiskImage
{
    private const int SectorSize = 512;
    private const uint TotalSectors = 131072;
    private const uint PartitionStartLba = 2048;
    private const uint ReservedSectors = 32;
    private const uint FatCount = 2;
    private const uint SectorsPerCluster = 1;
    private const uint RootCluster = 2;
    private const uint EfiDirectoryCluster = 3;
    private const uint BootDirectoryCluster = 4;
    private const uint FirstKernelCluster = 5;
    private const uint EndOfChain = 0x0FFFFFFF;

    private static readonly Guid EfiSystemPartitionType = new("C12A7328-F81F-11D2-BA4B-00A0C93EC93B");
    private static readonly Guid DiskIdentifier = new("4E6F7661-4F72-796E-8000-000000000026");
    private static readonly Guid PartitionIdentifier = new("4E6F7661-4F72-796E-8100-000000000026");

    internal static bool TryCreate(string kernelPath, string imagePath, out string error)
    {
        error = string.Empty;
        if (!File.Exists(kernelPath))
        {
            error = $"EFI application not found: {kernelPath}";
            return false;
        }

        long kernelLength = new FileInfo(kernelPath).Length;
        if (kernelLength <= 0)
        {
            error = "EFI application is empty.";
            return false;
        }

        uint lastUsableLba = TotalSectors - 34;
        uint partitionSectors = lastUsableLba - PartitionStartLba + 1;
        FatLayout layout = CalculateFatLayout(partitionSectors);
        uint clusterBytes = SectorsPerCluster * SectorSize;
        uint kernelClusters = checked((uint)((kernelLength + clusterBytes - 1) / clusterBytes));
        uint lastKernelCluster = checked(FirstKernelCluster + kernelClusters - 1);
        if (lastKernelCluster > layout.ClusterCount + 1)
        {
            error = $"EFI application is too large for the {TotalSectors * SectorSize / 1024 / 1024} MiB boot image.";
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(imagePath))!);
        using FileStream image = new(imagePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        image.SetLength((long)TotalSectors * SectorSize);

        WriteProtectiveMbr(image);
        WriteGuidPartitionTable(image, lastUsableLba);
        WriteFat32Volume(image, partitionSectors, layout, kernelPath, kernelLength, kernelClusters);
        image.Flush(true);
        return true;
    }

    private static FatLayout CalculateFatLayout(uint partitionSectors)
    {
        for (uint fatSectors = 1; fatSectors < partitionSectors / 2; fatSectors++)
        {
            uint dataSectors = checked(partitionSectors - ReservedSectors - FatCount * fatSectors);
            uint clusterCount = dataSectors / SectorsPerCluster;
            uint fatEntryCapacity = checked(fatSectors * SectorSize / 4);
            if (fatEntryCapacity >= clusterCount + 2)
            {
                return new FatLayout(fatSectors, clusterCount, ReservedSectors + FatCount * fatSectors);
            }
        }
        throw new InvalidOperationException("FAT32 geometry could not be calculated.");
    }

    private static void WriteProtectiveMbr(Stream image)
    {
        byte[] sector = new byte[SectorSize];
        int entry = 446;
        sector[entry + 0] = 0x00;
        sector[entry + 1] = 0x00;
        sector[entry + 2] = 0x02;
        sector[entry + 3] = 0x00;
        sector[entry + 4] = 0xEE;
        sector[entry + 5] = 0xFF;
        sector[entry + 6] = 0xFF;
        sector[entry + 7] = 0xFF;
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(entry + 8, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(entry + 12, 4), TotalSectors - 1);
        sector[510] = 0x55;
        sector[511] = 0xAA;
        WriteSector(image, 0, sector);
    }

    private static void WriteGuidPartitionTable(Stream image, uint lastUsableLba)
    {
        const uint partitionEntryCount = 128;
        const uint partitionEntrySize = 128;
        const uint primaryEntriesLba = 2;
        uint backupHeaderLba = TotalSectors - 1;
        uint backupEntriesLba = TotalSectors - 33;

        byte[] entries = new byte[checked((int)(partitionEntryCount * partitionEntrySize))];
        EfiSystemPartitionType.ToByteArray().CopyTo(entries, 0);
        PartitionIdentifier.ToByteArray().CopyTo(entries, 16);
        BinaryPrimitives.WriteUInt64LittleEndian(entries.AsSpan(32, 8), PartitionStartLba);
        BinaryPrimitives.WriteUInt64LittleEndian(entries.AsSpan(40, 8), lastUsableLba);
        byte[] name = Encoding.Unicode.GetBytes("NovaOryn ESP");
        name.CopyTo(entries, 56);
        uint entriesCrc = Crc32.Compute(entries);

        WriteAtLba(image, primaryEntriesLba, entries);
        WriteAtLba(image, backupEntriesLba, entries);

        byte[] primaryHeader = CreateGptHeader(1, backupHeaderLba, primaryEntriesLba, lastUsableLba, entriesCrc);
        byte[] backupHeader = CreateGptHeader(backupHeaderLba, 1, backupEntriesLba, lastUsableLba, entriesCrc);
        WriteSector(image, 1, primaryHeader);
        WriteSector(image, backupHeaderLba, backupHeader);
    }

    private static byte[] CreateGptHeader(uint currentLba, uint backupLba, uint entriesLba, uint lastUsableLba, uint entriesCrc)
    {
        byte[] sector = new byte[SectorSize];
        Encoding.ASCII.GetBytes("EFI PART").CopyTo(sector, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(8, 4), 0x00010000);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(12, 4), 92);
        BinaryPrimitives.WriteUInt64LittleEndian(sector.AsSpan(24, 8), currentLba);
        BinaryPrimitives.WriteUInt64LittleEndian(sector.AsSpan(32, 8), backupLba);
        BinaryPrimitives.WriteUInt64LittleEndian(sector.AsSpan(40, 8), 34);
        BinaryPrimitives.WriteUInt64LittleEndian(sector.AsSpan(48, 8), lastUsableLba);
        DiskIdentifier.ToByteArray().CopyTo(sector, 56);
        BinaryPrimitives.WriteUInt64LittleEndian(sector.AsSpan(72, 8), entriesLba);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(80, 4), 128);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(84, 4), 128);
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(88, 4), entriesCrc);
        uint headerCrc = Crc32.Compute(sector.AsSpan(0, 92));
        BinaryPrimitives.WriteUInt32LittleEndian(sector.AsSpan(16, 4), headerCrc);
        return sector;
    }

    private static void WriteFat32Volume(Stream image, uint partitionSectors, FatLayout layout, string kernelPath, long kernelLength, uint kernelClusters)
    {
        WriteFatBootSectors(image, partitionSectors, layout);
        WriteFatTables(image, layout, kernelClusters);
        WriteDirectories(image, layout, kernelLength);
        WriteKernel(image, layout, kernelPath, kernelClusters);
    }

    private static void WriteFatBootSectors(Stream image, uint partitionSectors, FatLayout layout)
    {
        byte[] boot = new byte[SectorSize];
        boot[0] = 0xEB;
        boot[1] = 0x58;
        boot[2] = 0x90;
        Encoding.ASCII.GetBytes("NOVAORYN").CopyTo(boot, 3);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(11, 2), SectorSize);
        boot[13] = (byte)SectorsPerCluster;
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(14, 2), (ushort)ReservedSectors);
        boot[16] = (byte)FatCount;
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(17, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(19, 2), 0);
        boot[21] = 0xF8;
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(22, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(24, 2), 63);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(26, 2), 255);
        BinaryPrimitives.WriteUInt32LittleEndian(boot.AsSpan(28, 4), PartitionStartLba);
        BinaryPrimitives.WriteUInt32LittleEndian(boot.AsSpan(32, 4), partitionSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(boot.AsSpan(36, 4), layout.FatSectors);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(40, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(42, 2), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(boot.AsSpan(44, 4), RootCluster);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(48, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(boot.AsSpan(50, 2), 6);
        boot[64] = 0x80;
        boot[66] = 0x29;
        BinaryPrimitives.WriteUInt32LittleEndian(boot.AsSpan(67, 4), 0x4E4F5626);
        Encoding.ASCII.GetBytes("NOVAORYN   ").CopyTo(boot, 71);
        Encoding.ASCII.GetBytes("FAT32   ").CopyTo(boot, 82);
        boot[510] = 0x55;
        boot[511] = 0xAA;

        byte[] fsInfo = new byte[SectorSize];
        BinaryPrimitives.WriteUInt32LittleEndian(fsInfo.AsSpan(0, 4), 0x41615252);
        BinaryPrimitives.WriteUInt32LittleEndian(fsInfo.AsSpan(484, 4), 0x61417272);
        BinaryPrimitives.WriteUInt32LittleEndian(fsInfo.AsSpan(488, 4), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32LittleEndian(fsInfo.AsSpan(492, 4), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32LittleEndian(fsInfo.AsSpan(508, 4), 0xAA550000);

        WriteVolumeSector(image, 0, boot);
        WriteVolumeSector(image, 1, fsInfo);
        WriteVolumeSector(image, 6, boot);
        WriteVolumeSector(image, 7, fsInfo);
    }

    private static void WriteFatTables(Stream image, FatLayout layout, uint kernelClusters)
    {
        byte[] fat = new byte[checked((int)(layout.FatSectors * SectorSize))];
        SetFatEntry(fat, 0, 0x0FFFFFF8);
        SetFatEntry(fat, 1, EndOfChain);
        SetFatEntry(fat, RootCluster, EndOfChain);
        SetFatEntry(fat, EfiDirectoryCluster, EndOfChain);
        SetFatEntry(fat, BootDirectoryCluster, EndOfChain);
        for (uint index = 0; index < kernelClusters; index++)
        {
            uint cluster = FirstKernelCluster + index;
            SetFatEntry(fat, cluster, index + 1 == kernelClusters ? EndOfChain : cluster + 1);
        }

        WriteAtLba(image, PartitionStartLba + ReservedSectors, fat);
        WriteAtLba(image, PartitionStartLba + ReservedSectors + layout.FatSectors, fat);
    }

    private static void WriteDirectories(Stream image, FatLayout layout, long kernelLength)
    {
        byte[] root = new byte[SectorSize];
        WriteDirectoryEntry(root, 0, "EFI        ", 0x10, EfiDirectoryCluster, 0);
        WriteCluster(image, layout, RootCluster, root);

        byte[] efi = new byte[SectorSize];
        WriteDirectoryEntry(efi, 0, ".          ", 0x10, EfiDirectoryCluster, 0);
        WriteDirectoryEntry(efi, 1, "..         ", 0x10, RootCluster, 0);
        WriteDirectoryEntry(efi, 2, "BOOT       ", 0x10, BootDirectoryCluster, 0);
        WriteCluster(image, layout, EfiDirectoryCluster, efi);

        byte[] boot = new byte[SectorSize];
        WriteDirectoryEntry(boot, 0, ".          ", 0x10, BootDirectoryCluster, 0);
        WriteDirectoryEntry(boot, 1, "..         ", 0x10, EfiDirectoryCluster, 0);
        WriteDirectoryEntry(boot, 2, "BOOTX64 EFI", 0x20, FirstKernelCluster, checked((uint)kernelLength));
        WriteCluster(image, layout, BootDirectoryCluster, boot);
    }

    private static void WriteKernel(Stream image, FatLayout layout, string kernelPath, uint kernelClusters)
    {
        byte[] buffer = new byte[SectorSize];
        using FileStream kernel = File.OpenRead(kernelPath);
        for (uint index = 0; index < kernelClusters; index++)
        {
            Array.Clear(buffer, 0, buffer.Length);
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = kernel.Read(buffer, offset, buffer.Length - offset);
                if (read == 0) break;
                offset += read;
            }
            WriteCluster(image, layout, FirstKernelCluster + index, buffer);
        }
    }

    private static void WriteDirectoryEntry(byte[] directory, int index, string shortName, byte attributes, uint firstCluster, uint size)
    {
        if (shortName.Length != 11) throw new InvalidOperationException($"FAT short name must contain 11 characters: {shortName}");
        Span<byte> entry = directory.AsSpan(index * 32, 32);
        Encoding.ASCII.GetBytes(shortName).AsSpan().CopyTo(entry);
        entry[11] = attributes;
        const ushort date = ((2026 - 1980) << 9) | (1 << 5) | 1;
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(16, 2), date);
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(18, 2), date);
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(20, 2), (ushort)(firstCluster >> 16));
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(24, 2), date);
        BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(26, 2), (ushort)firstCluster);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(28, 4), size);
    }

    private static void SetFatEntry(byte[] fat, uint cluster, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(fat.AsSpan(checked((int)(cluster * 4)), 4), value);
    }

    private static void WriteCluster(Stream image, FatLayout layout, uint cluster, byte[] data)
    {
        uint relativeLba = layout.DataStartSector + (cluster - 2) * SectorsPerCluster;
        WriteAtLba(image, PartitionStartLba + relativeLba, data);
    }

    private static void WriteVolumeSector(Stream image, uint relativeLba, byte[] sector)
    {
        WriteSector(image, PartitionStartLba + relativeLba, sector);
    }

    private static void WriteSector(Stream image, uint lba, byte[] sector)
    {
        if (sector.Length != SectorSize) throw new InvalidOperationException("Sector writes must contain exactly 512 bytes.");
        WriteAtLba(image, lba, sector);
    }

    private static void WriteAtLba(Stream image, uint lba, byte[] data)
    {
        image.Position = (long)lba * SectorSize;
        image.Write(data, 0, data.Length);
    }

    private readonly record struct FatLayout(uint FatSectors, uint ClusterCount, uint DataStartSector);
}

internal static class Crc32
{
    internal static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & (uint)-(int)(crc & 1));
            }
        }
        return ~crc;
    }
}
