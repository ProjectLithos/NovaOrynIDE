# NovaOryn 0.16.0

Adds native NVMe and AHCI/SATA storage drivers.

- PCI NVMe controller discovery, reset/enable sequence, admin submission/completion queues, I/O submission/completion queues, Identify Controller, namespace discovery, namespace block-device registration, synchronous block read/write/flush, and MSI/MSI-X programming APIs with polling fallback.
- AHCI PCI controller discovery, ABAR mapping, SATA link/signature discovery, DMA command-list/FIS/command-table setup, ATA IDENTIFY DEVICE, LBA48 capacity, logical-sector sizing, READ DMA EXT, WRITE DMA EXT, and FLUSH CACHE/FLUSH CACHE EXT.
- Each NVMe namespace and SATA disk is registered as an independent NovaOryn block device.
- Generated command-line and Visual Studio kernel templates include both assemblies and initialize NVMe before AHCI and VirtIO storage.
- Independent NVMe and AHCI methodology test programs are added to the main build.
