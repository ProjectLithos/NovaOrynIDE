# Nova Oryn OS SDK 0.0.38

## Project-template contents

The NovaOryn Visual Studio project template now explicitly creates and includes:

- `Kernel.cs`
- `CoreLib.cs`
- `BootContext.cs`
- `FramebufferConsole.cs`
- `BitmapFont.cs`
- `NovaOrynProject.json`

The generated project disables implicit MSBuild items and declares each source item explicitly. This removes the template-engine ambiguity that previously created only the renamed `.csproj`. The VSIX build now validates the final archive entries before publishing the installer artifact.

## Build and run separation

A normal `Build-NovaOryn.bat` invocation finishes after producing the managed object, EFI application and bootable FAT32 image. It does not start QEMU.

Use one of these explicit run paths:

```text
Build-NovaOryn.bat -Run
Build-NovaOryn.ps1 -Run
Visual Studio: NovaOryn Run / F5 / Ctrl+F5
```

The Visual Studio Build command does not pass `-Run`; its Run command does.
