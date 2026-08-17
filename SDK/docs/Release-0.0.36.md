# NovaOryn 0.0.36

## Visual Studio template packaging correction

- Packages the complete NovaOryn kernel template as a single VSIX project-template ZIP.
- The generated project now contains `Kernel.cs`, `CoreLib.cs`, `BootContext.cs`, `FramebufferConsole.cs`, `BitmapFont.cs`, and `NovaOrynProject.json`.
- Uses QEMU's SDL display backend on Windows to avoid the observed GTK/GDK window failure.
- Preserves serial acceptance and the indefinite `CPU.Halt()` loop.
