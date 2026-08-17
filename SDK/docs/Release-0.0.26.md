# Nova Oryn OS SDK 0.0.26

## Purpose

This release implements the first complete boot-and-run acceptance stage for the freestanding x64 NativeAOT kernel produced by NovaOryn.

## Implemented pipeline

Running `Build-NovaOryn.bat` now performs:

```text
Roslyn managed-IL build
    -> direct repository-pinned ILC compilation
    -> LLD EFI link
    -> GPT/FAT32 boot-image creation
    -> OVMF/QEMU launch
    -> serial runtime acceptance
    -> halted VM remains open
```

## OVMF discovery and installation

`Install-NovaOrynToolchain.ps1` now locates the x64 OVMF firmware distributed with QEMU. It accepts the standard QEMU names:

```text
edk2-x86_64-code.fd
edk2-i386-vars.fd
```

and the compatible fallback names:

```text
OVMF_CODE.fd
edk2-x86_64-vars.fd
OVMF_VARS.fd
```

If QEMU exists without the required firmware, the installer repairs the QEMU package through `winget`, scans again, and records the resolved paths as `ovmfCodeX64` and `ovmfVarsX64` in `.toolchain\NovaOryn.ToolPaths.json`.

## Real bootable image

`NovaOryn.ImageBuilder.exe` no longer writes an image plan. It now creates a 64 MiB raw disk image containing:

- a protective MBR;
- primary and backup GPT metadata;
- one EFI System Partition;
- a FAT32 filesystem;
- the linked kernel at `EFI\BOOT\BOOTX64.EFI`.

The default output is:

```text
Artifacts\MinimalKernel\MinimalKernel.img
```

The executable also writes:

```text
Artifacts\MinimalKernel\BootFiles\EFI\BOOT\BOOTX64.EFI
Artifacts\MinimalKernel\NovaOryn.Image.json
```

## QEMU runtime acceptance

`NovaOryn.QemuLauncher.exe` starts `qemu-system-x86_64.exe` with:

- the `q35` machine;
- x64 OVMF code as read-only pflash;
- a private writable copy of the OVMF variable store;
- the generated GPT/FAT32 image as the first boot device;
- COM1 redirected to a serial log;
- no `-S` option;
- `-no-reboot` and `-no-shutdown`.

Each launch receives a unique directory below:

```text
Artifacts\MinimalKernel\Runs\<run-id>
```

This prevents the running VM from locking the canonical build image or the next run's variable store.

## Runtime acceptance contract

The no-CoreLib bootstrap now emits exactly:

```text
NovaOryn KMain started.
CPU halted.
```

The launcher waits for both lines, then verifies that QEMU remains alive after the halt message. On success it writes:

```text
Artifacts\MinimalKernel\serial.log
Artifacts\MinimalKernel\NovaOryn.Run.json
```

The launcher then returns successfully without closing QEMU. The native `CLI` plus repeating `HLT` loop therefore leaves the VM open indefinitely.

## Build controls

Default build and run:

```text
Build-NovaOryn.bat
```

Build and create the boot image without launching QEMU:

```text
Build-NovaOryn.bat -NoRun
```

Override the runtime acceptance timeout:

```text
Build-NovaOryn.bat -BootTimeoutSeconds 60
```
