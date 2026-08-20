# NovaOryn IDE 0.12.0

NovaOryn IDE 0.12.0 adds the **QEMU Hardware Test Matrix** to the existing Test Explorer.

## Automated hardware coverage

The matrix can exercise:

- 1, 2, 4 and 8 virtual CPUs.
- 128, 512, 1024 and 2048 MiB RAM.
- VirtIO block, AHCI and NVMe boot storage.
- VirtIO-net and Intel E1000 networking.
- UEFI GOP/standard VGA and VirtIO GPU graphics paths.
- xHCI with USB keyboard/mouse and a no-USB control case.
- UEFI plus legacy BIOS when a BIOS-bootable NovaOryn artifact exists.

## Balanced and Full modes

**Balanced matrix** varies individual hardware dimensions around a stable baseline, then runs a combined high-end stress case. This keeps routine validation useful and makes a failure easier to attribute to one hardware choice.

**Full Cartesian matrix** generates every supported combination of the CPU, RAM, storage, network, graphics, USB and available firmware axes.

The OS is built once before matrix execution. Every case then runs in a clean QEMU process with an independent serial log. A case passes only when both `NovaOryn KMain started.` and the interactive `NovaOryn> ` prompt are observed. QEMU is terminated after the acceptance result so the next case starts cleanly.

## Results

The Test Explorer shows live per-case state and streaming matrix output. A machine-readable `NovaOryn.QemuHardwareMatrix.json` report is written below `.novaoryn/tests/qemu-hardware-matrix` in the operating-system project.

The current x64 pipeline emits an EFI/UEFI image. When a legacy BIOS boot artifact does not exist, BIOS is recorded as **not applicable / skipped**; NovaOryn does not falsely report that firmware mode as tested.
