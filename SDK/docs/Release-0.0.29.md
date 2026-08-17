# Nova Oryn OS SDK 0.0.29

Release 0.0.29 separates the end-user C# kernel project from the SDK source tree.

The default project is created at `C:\Users\<UserName>\Source\Repos\NovaOrynKernel` by the compiled `NovaOryn.ProjectCreator` tool. It contains `NovaOrynKernel.sln`, `NovaOrynKernel.csproj`, `NovaOrynProject.json`, the freestanding CoreLib surface, boot context, managed framebuffer console, bitmap font and editable `Kernel.cs`.

`Build-NovaOryn.bat` automatically creates the project on first use and thereafter compiles the files from that external directory. It does not overwrite existing user files. Build artifacts continue to be written beneath `C:\NovaOryn\Artifacts`.
