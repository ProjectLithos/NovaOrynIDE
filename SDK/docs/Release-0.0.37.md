# NovaOryn 0.0.37

NovaOryn 0.0.37 corrects project-template discovery in Visual Studio.

## Correction

The 0.0.36 VSIX contained the template files but declared a single standalone ZIP as the `Microsoft.VisualStudio.ProjectTemplate` asset. Visual Studio installed the extension but did not index that layout in the Create a new project catalogue.

The VSIX now follows the proven Oryn OS SDK layout:

- the project-template asset points to `ProjectTemplates`;
- `NovaOryn.VisualStudio.Project.vstman` is packaged at the root of that tree;
- the manifest points to `CSharp\1033\NovaOrynKernel`;
- the `.vstemplate`, `.csproj`, icon, kernel sources and project manifest are packaged as loose VSIX content;
- no redundant nested template ZIP is generated or declared.

After installing 0.0.37 and restarting Visual Studio, searching for `NovaOryn` displays **NovaOryn Kernel 0.0.37**. Creating it includes `Kernel.cs`, `CoreLib.cs`, `BootContext.cs`, `FramebufferConsole.cs`, `BitmapFont.cs`, and `NovaOrynProject.json`.

The QEMU SDL launch path and working `CPU.Halt()` acceptance remain unchanged.
