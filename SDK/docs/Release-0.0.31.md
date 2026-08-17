# NovaOryn 0.0.31 — Visual Studio Kernel Template

This release derives the proven Oryn OS SDK Visual Studio extension pattern for NovaOryn.

## Included

- `NovaOryn.VisualStudio.vsix` source project.
- **NovaOryn Kernel 0.0.31** C# project template.
- F5 and Ctrl+F5 interception for NovaOryn kernel projects.
- Visual Studio Output pane streaming from `Build-NovaOryn.ps1`.
- Tools menu commands for **Build Kernel** and **Run Kernel**.
- SDK-root discovery through `NOVAORYN_SDK_ROOT`, defaulting to `C:\NovaOryn`.

## Use

1. Run `Install-NovaOrynVSIX.bat`.
2. Close all Visual Studio instances when the VSIX installer requests it.
3. Install the extension into the desired Visual Studio edition.
4. In Visual Studio, select **Create a new project** and search for **NovaOryn Kernel**.
5. Create the project under `C:\Users\<UserName>\Source\Repos`.
6. Press Ctrl+F5 or F5 to build, create the EFI image, boot QEMU, capture serial output and leave the halted VM open.

F5 currently runs the QEMU acceptance path; source-level managed kernel debugging will be introduced separately.
