# Nova Oryn OS SDK 0.0.39

## Structured kernel template

New NovaOryn kernel projects now organise source files into purpose-specific directories:

```text
NovaOrynKernel
├── Boot
│   └── BootContext.cs
├── Console
│   ├── BitmapFont.cs
│   └── FramebufferConsole.cs
├── Kernel
│   └── Kernel.cs
├── Runtime
│   └── CoreLib.cs
├── NovaOrynKernel.csproj
└── NovaOrynProject.json
```

Both the Visual Studio VSIX template and the command-line project creator use this layout. The project files explicitly include the nested source paths, so Solution Explorer displays the folders and the freestanding build receives the same source set as before.

The build/run separation introduced in 0.0.38 remains unchanged: a normal build does not start QEMU; Run, F5 and Ctrl+F5 do.
