# Nova Oryn OS SDK 0.0.41

## Visual Studio nested-template correction

Visual Studio was combining each `<Folder>` target with a project-item path that repeated the same directory. For example, the `Kernel` folder contained a `Kernel\Kernel.cs` item, causing Visual Studio to search for `Kernel\Kernel\Kernel.cs` in its temporary template directory.

The project template now uses filenames relative to their enclosing `<Folder>` elements:

- `Kernel/Kernel.cs` is declared as `Kernel.cs` inside the `Kernel` folder.
- `Runtime/CoreLib.cs` is declared as `CoreLib.cs` inside the `Runtime` folder.
- `Boot/BootContext.cs` is declared as `BootContext.cs` inside the `Boot` folder.
- Console files are declared by filename inside the `Console` folder.

This preserves the structured project layout while allowing Visual Studio to instantiate the complete kernel project successfully. Existing build-only and explicit-run behaviour is unchanged.
