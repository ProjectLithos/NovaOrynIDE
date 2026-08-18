# NovaOryn IDE 0.3.1

## Image / Disk Explorer

NovaOryn IDE 0.3.1 adds a native **Image / Disk Explorer** under **NovaOryn > Engineering**.

- Discovers OS-project and bundled SDK disk artifacts (`.img`, `.raw`, `.iso`, `.vhd`, `.vhdx`, `.bin`).
- Identifies raw/MBR/GPT images, protective MBRs and GPT disk/partition GUIDs.
- Enumerates GPT and MBR partitions, including EFI System Partition recognition.
- Detects FAT32 volumes and reports geometry, volume labels and free clusters.
- Browses FAT32 directory trees including long-file-name entries.
- Reads individual FAT32 files directly from their cluster chains without host mounting.
- Provides bounded raw-disk and file hex/ASCII inspection with decimal or hexadecimal offsets.
- Restricts reads to the active NovaOryn OS project and bundled SDK artifact roots.

NovaOryn IDE is now version **0.3.1**.
