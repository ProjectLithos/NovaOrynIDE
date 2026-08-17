# Nova Oryn OS SDK 0.0.27

## Managed framebuffer console

Release 0.0.27 adds the first visible managed NovaOryn console while preserving the successful 0.0.26 serial and halt acceptance path.

## UEFI Graphics Output Protocol capture

`native/x64/Entry.asm` now receives the normal x64 UEFI `EFI_SYSTEM_TABLE` pointer and uses `EFI_BOOT_SERVICES.LocateProtocol` to locate the active Graphics Output Protocol. It reads the current mode and records:

- framebuffer base address;
- framebuffer byte length;
- horizontal and vertical resolution;
- pixels per scan line;
- UEFI pixel format;
- red, green, blue, and reserved channel masks.

The resulting native boot context is passed in RCX to the ILC-exported `NovaOrynManagedEntry`. Firmware calls are complete before interrupts are disabled and before managed execution begins.

## Managed validation and rendering

The freestanding no-CoreLib bootstrap performs all safety checks before writing to video memory. It rejects:

- a missing or incorrectly signed boot context;
- a null or unaligned framebuffer address;
- zero dimensions or byte length;
- a pitch smaller than the visible width;
- a framebuffer whose pitch and height exceed the supplied byte length;
- UEFI `PixelBltOnly` mode;
- invalid or overlapping direct-colour masks.

The managed console then clears the framebuffer with a dark background and renders a built-in 5x7 bitmap font. A two-times scale is selected on normal OVMF resolutions and a one-times fallback is retained for smaller modes.

## Mirrored acceptance output

Every character is written to COM1 first and then to the framebuffer. The required output remains exactly:

```text
NovaOryn KMain started.
CPU halted.
```

After both lines are visible and captured in `serial.log`, the kernel enters the existing repeating `CLI`/`HLT` loop. QEMU must remain open indefinitely.

## SDK assembly

The solution now includes `NovaOryn.Console.Framebuffer`. Its `FramebufferConsole` implements `IConsole`, validates a `BootContext`, supports RGB, BGR, and bit-mask pixel layouts, clears the framebuffer, and renders text with the same minimal bitmap font methodology. `NovaOryn.Kernel.Sample` demonstrates explicit mirroring to `SerialConsole` and `FramebufferConsole`.

## Acceptance

Run:

```text
Build-NovaOryn.bat
```

Expected results:

```text
[ OK ] Managed KMain execution confirmed.
[ OK ] CPU.Halt() output confirmed.
[ OK ] QEMU remains open indefinitely.
[ OK ] NovaOryn x64 NativeAOT boot-and-run acceptance completed.
```

The QEMU display must show the two acceptance lines instead of remaining on the TianoCore boot screen, and `Artifacts\MinimalKernel\serial.log` must contain the same lines.
