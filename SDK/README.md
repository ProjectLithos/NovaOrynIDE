# Nova Oryn OS SDK 0.37.0

## Release 0.37.0

0.37.0 fixes the NativeAOT debug relocation-anchor link check. Debug builds now validate `NovaOrynDebugImageAnchor` in `Entry.obj` and derive its final linked address from the EFI PE32+ entry point, avoiding unreliable `llvm-nm` lookup on the fully linked PE image. Source-line PDB/map generation from 0.36.8 remains enabled.

## Release 0.36.3

0.36.3 corrects the generated-kernel VirtIO GPU startup contract. VirtIO GPU is a driver-owned graphics target, not a GUI dependency: driver-enabled kernels now reference and initialize `NovaOryn.Kernel.Virtio.Gpu` through the HAL even when the GUI work area is excluded. `BootStartup` now contains only unconditional core-startup dependencies, preventing optional work-area assemblies from leaking into the core boot contract.

## Release 0.36.2

0.36.2 corrects Project Configuration semantics: the areas checked under “What do you want to work on?” are the only deliberately missing areas. All unchecked areas are supplied by NovaOryn and placed according to the selected Monolithic, Microkernel, or Hybrid topology. NovaOryn.Configuration.json is authoritative during refresh/build.

## Release 0.35.25

0.35.25 fixes the remaining Visual Studio threading analyzer error in the Configuration Pages integration. `NovaOrynProjectRecognizer.TryGetProjectDirectory(Project)` now calls `ThreadHelper.ThrowIfNotOnUIThread()` before reading `EnvDTE.Project.FullName`, satisfying VSTHRD010 and preserving the UI-thread contract already used by the configuration service.

## Release 0.35.24

0.35.24 fixes the Visual Studio Configuration Pages VSIX compilation introduced in 0.35.23. `NovaOrynProjectRecognizer` now exposes the project-directory helper used by the configuration service; selected-project discovery uses the supported `DTE.SelectedItems` automation collection instead of `DTE.ToolWindows`; and the delayed first-run configuration task is observed with `FileAndForget`, satisfying VSTHRD110. These corrections allow the 0.35.23 Architecture / Kernel Model / Work Areas / Summary configuration UI to be compiled and installed.

## Release 0.35.23

0.35.23 adds NovaOryn Visual Studio **Project Configuration Pages**. New kernel projects open a four-page configuration window for Architecture, Kernel Model, Work Areas and Summary. Settings are persisted into `NovaOryn.Configuration.json`, generated MSBuild properties in `NovaOryn.Configuration.props`, and `NovaOrynProject.json`. Architecture choices are x64, ARM64 and RISC-V 64; x64 is currently buildable, while unsupported architecture targets are retained but cause an explicit build stop rather than silently producing x64. Kernel models are Monolithic, Microkernel and Hybrid, with Kernel/Userland/Mixed default execution-domain metadata. Work areas include Shell, GUI, Drivers, HAL, Audio, Filesystems, Storage, Networking, USB, Input, Processes, Scheduler, System Calls, Security, Diagnostics and Tests.

## Release 0.35.22

0.35.22 fixes incremental/overlay upgrades from the old built-in FAT32 implementation to the selectable-filesystem architecture. If a pre-0.35.21 `KernelFat32.cs` remains on disk, `Build-NovaOryn.ps1` removes it before compilation. `NovaOryn.Kernel.Storage.csproj` also explicitly excludes `KernelFat32.cs`, so stale source cannot enter the generic storage assembly even if an overlay failed to delete it. The optional `NovaOryn.Filesystem.FatFs` project remains the only FAT implementation supplied by the SDK.

## Release 0.35.21

0.35.21 removes the built-in `KernelFat32` filesystem from the generic storage assembly and makes filesystem choice an explicit end-user SDK decision. The base kernel now initializes only `KernelStorage`/`KernelVfs`; it installs no filesystem automatically. A ninth independent Visual Studio template, **NovaOryn Filesystem - FatFs**, provides a selectable C#/.NET-compatible FatFs port for FAT12/FAT16/FAT32. The first profile supports 8.3 traversal, reads, VFS seek, flush and safe in-place writes to existing allocated files. exFAT, long-file-name mutation and create/delete/rename/truncate are not advertised until the generic VFS exposes the required operations.

## Release 0.35.20

0.35.20 replaces PS/2 hardware typematic delivery with NovaOryn-controlled keyboard repeat. The PS/2 decoder now reports only genuine key-down/key-up transitions and exposes current key state; repeated hardware make codes are suppressed. The console implements a shared software repeat policy for PS/2 and USB HID keyboards: an action fires once on key-down, begins repeating after 300 ms at a 40 ms interval, and is cancelled immediately by the matching key-up. Physical input is drained before repeat work on every 1 ms console-service tick, and repeat deadlines are scheduled from the current time rather than caught up, so slow framebuffer scrolling cannot leave queued repeat actions continuing after the user releases the key.

## Release 0.35.19

0.35.19 makes empty user-kernel recovery authoritative at the final build boundary. `Build-NovaOryn.ps1` now repairs a missing/empty selected or external `Kernel\Kernel.cs` directly from the canonical high-level SDK template immediately before the low-level-token safety scan and compilation, then verifies the resulting byte length. `NovaOryn.ProjectCreator` performs the same final repair/verification before reporting refresh success. The Visual Studio kernel template no longer auto-opens `Kernel\Kernel.cs` during creation, preventing an editor buffer from writing stale/empty contents back over the externally refreshed source. Non-empty user-owned kernel code is never replaced.

## Release 0.35.18

0.35.18 fixes the Visual Studio Run failure `You cannot call a method on a null-valued expression` after project refresh. The failure occurred when `Kernel\Kernel.cs` existed but was zero-length/whitespace: Windows PowerShell could return `$null` from `Get-Content -Raw`, and the build then called `.IndexOf` on it. `NovaOryn.ProjectCreator` now re-seeds only a completely empty required user `Kernel\Kernel.cs` from the SDK template while preserving every non-empty user implementation. The build uses `File.ReadAllText`, validates the source before scanning it, and reports a precise empty-source error rather than a PowerShell null-method exception.

## Release 0.35.17

0.35.17 fixes project creation where Visual Studio created the main NovaOryn kernel project but omitted nested SDK/Userland `.csproj` descriptors. VSIX packaging now uses a disposable staging tree: the template's primary project remains the one raw `.csproj`, while every nested project descriptor is stored as `<name>.csproj.template`; the generated `.vstemplate` restores the original `.csproj` path through `ProjectItem TargetFileName`. The source tree itself remains normal `.csproj` files. The build validates every neutral payload and source-to-target mapping before the VSIX is produced.

## Release 0.35.16

0.35.16 fixes the final VSIX packaging stage for the eight independent NovaOryn project templates. VSSDK template registration is retained, but the build now deterministically embeds every generated template ZIP into the finished VSIX under `ProjectTemplates/`, repairs/verifies the corresponding `Microsoft.VisualStudio.ProjectTemplate` assets and OPC `application/zip` content types, and reopens the completed package to prove all eight physical payloads are present. The VSIX project also marks generated template ZIP content with `CopyToOutputDirectory=PreserveNewest`.

## Release 0.35.15

0.35.15 replaces the fragile Visual Studio multi-project `ProjectGroup` template with eight independent, ordinary C# project templates: **NovaOryn Kernel**, **NovaOryn Kernel Driver**, **NovaOryn Kernel Library**, **NovaOryn Userland Application**, **NovaOryn Userland Service**, **NovaOryn Userland Driver**, **NovaOryn Userland Library**, and **NovaOryn Test Project**. Each template is independently packaged and registered by the VSIX and is also installed into the Visual Studio user project-template catalogue.

The generated kernel is a configuration-driven workspace. Folder location never automatically links `KernelProjects\**\*.csproj` into the kernel: `NovaOryn.Configuration.json` generates the authoritative props/targets and active project graph. Userland and test projects remain independently compiled through the generated workspace project list. The Visual Studio synchronizer shows only active configured projects while preserving inactive project files on disk.

## Release 0.35.14

0.35.14 corrects the Visual Studio multi-project template ZIP layout. The visible `NovaOrynKernel.vstemplate` is now the only `.vstemplate` at the archive root, while the main hidden kernel child and all files that belong to it are stored under `KernelProject\`. This follows Visual Studio's documented multi-project-template structure and prevents the root ProjectGroup from competing with a hidden child template during catalogue discovery. The VSIX build now rejects any generated template ZIP that contains zero or more than one root `.vstemplate`.

## Release 0.35.13

0.35.13 fixes Visual Studio MSBuild SDK resolution for the VSIX build. Visual Studio's `MSBuild.exe` is still used so that the VSSDK template-registration targets run, but its .NET SDK resolver is now explicitly bound to NovaOryn's repository-pinned `.toolchain\DotNet` installation and the SDK version specified by `global.json`. The build no longer depends on a matching machine-wide SDK under `C:\Program Files\dotnet`.

## Release 0.35.12

0.35.12 corrects Visual Studio project-template catalogue registration. The VSIX is now built with the selected Visual Studio installation's own `MSBuild.exe`, and the build fails unless Microsoft.VSSDK.BuildTools produces `obj\Release\templateFiles.json` containing the NovaOryn template registration. The fallback user template is installed directly in the `ProjectTemplates` root rather than the `Visual C#` child directory. Installation also clears project, item and component-model caches and runs both `devenv /updateconfiguration` and `devenv /installvstemplates`.

## Release 0.35.11

0.35.11 fixes Visual Studio project-template catalogue discovery. It removes the duplicate `TemplateID` shared by the visible ProjectGroup and its hidden kernel child, corrects the Visual Studio template-manifest schema to use `VSTemplateType`, embeds the corrected `.vstman` alongside the template ZIP, installs the user template in the documented `ProjectTemplates\Visual C#` directory, clears stale project-template catalogue caches, and rebuilds the selected Visual Studio instance's template catalogue.

## Release 0.35.10

0.35.10 fixes side-by-side Visual Studio discovery. The VSIX installer now enumerates installed Visual Studio instances with `vswhere`, selects the newest supported instance (so Visual Studio 18/2026 wins over an older VS 2022 installation), installs the project template into the matching user `ProjectTemplates` directory, removes positively identified legacy `Oryn OS Project` template ZIPs, and rebuilds that selected instance's template cache.

## Release 0.35.9

0.35.9 fixes Visual Studio template discovery by installing the validated multi-project ZIP directly in the configured user ProjectTemplates root and explicitly rebuilding Visual Studio's template cache after installation.

## Release 0.35.6

0.35.6 made VSIX template packaging deterministic at the physical-package level: after VSSDK created the extension container, NovaOryn explicitly embedded `ProjectTemplates/NovaOrynKernel.zip` and registered its package content type before validating the final VSIX.

## Release 0.35.5

0.35.5 packages the Visual Studio ProjectGroup as a single compressed multi-project template, so the Userland child projects receive their actual command/settings/fonts/images/drivers source files instead of appearing as empty project shells. VSIX validation now opens the nested template archive and verifies every required child source item before the extension is published.
## Release 0.35.0

0.35.0 adds a blinking framebuffer command caret and a persistent scrollback scrollbar, restructures generated Visual Studio kernels into high-level Kernel, Boot and HAL layers, and introduces a first-class src/Userland aggregate with Commands, Settings, Fonts, Images and Drivers sub-projects.

## Release 0.34.2

0.34.2 fixes the two live integration failures found after the interactive console and Linux font pack were introduced: PS/2 input is now hardware-IRQ driven through the I/O APIC/interrupt broker, and the Linux font converter supports both VGA-style and Sun-style kernel font source tables.

## Release 0.34.0

0.34.0 adds a real keyboard-driven `NovaOryn> ` command line for the QEMU/GOP framebuffer console. PS/2 and USB HID events now feed a shared freestanding line editor with Backspace, Enter, help/echo, and the existing font, buffering, and keyboard controls. Bare digits are usable as command input; Ctrl+1/2/3 force buffering and Alt+1/2/3 force font size.

## Release 0.33.0

0.33.0 completes the initial display-driver stage without duplicating the existing 0.28-era graphics work. UEFI GOP remains the generic firmware framebuffer; an explicit `SimpleFramebuffer` adapter now registers VESA-like/bootloader/platform linear framebuffers as first-class graphics targets; and VirtIO GPU remains NovaOryn's first proper driver-owned display path with QEMU testing, framebuffer resources, dirty-region presentation, and runtime mode/resource changes. Native AMD/NVIDIA/Intel drivers remain intentionally deferred.

## Release 0.32.0

0.32.0 fixes framebuffer-console scrolling and presentation performance. The normal console policy is now automatic and resolves to double buffering for text output; single/double/triple modes remain forceable for diagnostics. Framebuffer writes are batched by logical string/line/region rather than presented per character, automatic live scrolling moves framebuffer rows by one active text-line height and clears only the exposed strip, and scrollback viewport changes perform one completed-frame presentation. Serial output remains immediate.

## Release 0.31.1

0.31.1 fixes the PSF2 console-font fallback compilation path introduced in 0.30.0/0.31.0. The fallback renderer now passes the existing `Byte` glyph value directly to `BitmapFont.GetGlyphRow`, matching the kernel console bitmap-font API and allowing the selected-kernel fast build to compile `NovaOryn.Kernel.Console` successfully.

## Release 0.30.0

0.30.0 makes framebuffer fonts a first-class console resource. The QEMU/UEFI GOP console can install and render validated PSF2 font faces from kernel-accessible memory, including PSF2 Unicode tables for ASCII console characters. Font face and rendered font size are independent: the existing 1/2/3 controls remain 8/16/24 pixel size presets, with preset 3 still the default. Changing the face or size reflows and redraws retained output. The existing embedded NovaOryn Mono face remains the guaranteed early-boot and recovery fallback.

## Release 0.29.0

0.29.0 separates normal kernel development from exhaustive SDK validation. Visual Studio Build/Run and `Build-NovaOryn.bat` now use a fast kernel path: they build only the required NovaOryn host tools and then compile the selected kernel project, allowing MSBuild to follow that project's referenced SDK dependency graph. They no longer regenerate documentation, build `NovaOryn.sln`, or build/run every independent test program before each kernel build.

Run `Validate-NovaOryn.bat` when you explicitly want the full SDK solution build, documentation generation/audit, policy programs, and independent subsystem tests before a release or major integration checkpoint.

## Release 0.28.2

0.28.2 is a display-driver build-correction release. It fixes the remaining VirtIO GPU queue-size type mismatch by passing the compile-time queue size as a `UInt16`-compatible constant to `SetupQueue`. The graphics architecture, GOP fallback, VirtIO GPU protocol implementation, and public graphics contracts are otherwise unchanged.


## Release 0.28.0

0.28.0 introduces the display-driver architecture. UEFI GOP is retained as the boot-safe generic framebuffer target through `NovaOryn.Kernel.Graphics`, while `NovaOryn.Kernel.Virtio.Gpu` adds NovaOryn's first proper driver-owned graphics device for QEMU. The VirtIO GPU path implements modern PCI transport discovery, display-info discovery, 2D framebuffer resources, backing attachment, scan-out selection, transfer/flush presentation, and runtime mode/resource changes. AMD, NVIDIA, and Intel native GPU drivers remain intentionally out of scope for this stage.

# Nova Oryn OS SDK 0.27.2

## Release 0.27.2

0.27.2 corrects the remaining USB solution-build issues exposed after the 0.27.1 bus fix: xHCI route-string shift typing is valid C#, and USB mass-storage and hub discovery no longer place `stackalloc` inside loops. The modular USB subsystem remains split across `NovaOryn.Bus.Usb`, `NovaOryn.Usb.Xhci`, `NovaOryn.Usb.Hid`, `NovaOryn.Usb.MassStorage`, and `NovaOryn.Usb.Hub`.

## Release 0.26.1

0.26.1 optimizes software double/triple framebuffer presentation with dirty-region tracking. Normal character output now copies only the modified glyph region instead of copying the entire framebuffer once (double buffering) or twice (triple buffering) for every character. Font preset 3 and triple buffering remain the defaults.

## Release 0.26.0

0.26.0 introduced software single/double/triple framebuffer buffering and userland `font`/`buffering` Get/Set commands. Both font and buffering default to preset 3.

## Release 0.25.6

0.25.6 corrects the build-policy regression introduced in 0.25.5. The PS/2 contract policy now reads the authoritative bootstrap source before performing the no-runtime-contract-comparison assertion, so the policy program itself compiles and can validate the intended architecture.

The modular managed-library work from 0.25.0, including `NovaOryn.String`, and the runtime PS/2 implementation are unchanged.

See `docs/Release-0.25.6.md`, `docs/Release-0.25.5.md`, and `docs/Managed-Libraries.md`.


## Dispatch model

NovaOryn separates optional polling (`NovaOryn.Kernel.Polling`) from normal timer-driven (`NovaOryn.Kernel.TimerDispatch`) and interrupt-driven (`NovaOryn.Kernel.InterruptDispatch`) execution. Generated kernels do not enable background polling.
