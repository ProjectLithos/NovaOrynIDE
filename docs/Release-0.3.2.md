# NovaOryn IDE 0.3.2

## Image / Disk Explorer discovery fix

- Excludes QEMU `debugcon.bin` rendezvous files from disk-image discovery.
- Ignores generic `.bin` files smaller than 1 MiB unless they contain a recognised disk/filesystem signature.
- Recognises raw FAT32 images during discovery in addition to GPT, MBR, ISO 9660 and VHDX signatures.
- Prioritises project images, recognised disk formats, newer artifacts and larger images so the useful boot image is selected first.
- Clarifies in the Image / Disk Explorer UI that tiny debugger/telemetry binaries are intentionally ignored.
