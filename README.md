NovaOryn IDE 0.1.47 bundles NovaOryn SDK 0.37.4 and uses a QEMU debugcon relocation rendezvous so exact C# breakpoints are armed before KMain without relying on guest INT3 handling.

# NovaOryn IDE

### 0.1.30 Existing OS reconfiguration

NovaOryn IDE 0.1.30 adds an authoritative **Reconfigure OS** workflow for an operating system that is already open. Use either the new **NovaOryn > Reconfigure OS** menu command or right-click the operating-system root folder in Explorer and choose **Reconfigure NovaOryn OS**.

The configurator reloads the current `NovaOryn.json` selections instead of starting from defaults. Applying the configuration rebuilds `NovaOryn.ProjectGraph.json`, generated component projects, `Configuration/GeneratedConfiguration.cs`, `NovaOryn.slnx`, the SDK bridge manifest, and the generated Build/Run launchers. Generated components that are no longer selected have only their generator-owned `.csproj` and `GeneratedFeature.cs` artifacts removed; directories containing user files are retained.

`Kernel\Kernel.cs` is explicitly user-owned during reconfiguration and is never regenerated or replaced. The OS name and root path are also fixed during reconfiguration so changing kernel architecture, drivers, services, filesystems, networking, userland, GUI, testing or safety options cannot accidentally turn the operation into a project rename.

The Run/Debug toolbar fixes from 0.1.12 remain in place: the toolbar stays below the menu, uses the standard Theia `ApplicationShell`, saves dirty editors before build/run, and streams SDK output inside the IDE.


NovaOryn IDE is the desktop development environment for the NovaOryn Operating System SDK. It is a custom Eclipse Theia desktop application with NovaOryn-specific project configuration and generation.

## Current release: 0.1.47

### 0.1.47 Professional OS engineering workspace

NovaOryn IDE 0.1.47 adds four first-class operating-system engineering tools. The **OS Dashboard** becomes the default view after explicitly opening an existing NovaOryn operating system and summarizes the kernel model, architecture, scheduler, syscall model, target settings, driver count, test count, hardware configuration and diagnostics.

The new **NovaOryn Console** is a dedicated bottom panel for build, QEMU and serial/kernel output. It supports filtering, pause/resume, clear and auto-scroll. Debug QEMU serial output is tailed from the active run and labelled as kernel output so boot/runtime messages remain visible inside the IDE.

The **Hardware / Device Tree** presents configured CPU/SMP, interrupt/timer, platform/bus, storage, networking, input, graphics and audio devices as an OS-specific tree. The **Test Explorer** discovers individual C# test executable projects from both the operating-system Tests folder and the bundled SDK tests, runs one with NovaOryn's pinned .NET SDK, streams output and reports PASS/FAIL.

All four views are available under the **NovaOryn > Engineering** menu.

## Previous release: 0.1.46

### 0.1.46 Page-table/heap inspection and crash-dump debugging

NovaOryn IDE 0.1.46 adds an x64 **Page Tables** inspector that walks the active CR3 through PML4, PDPT, PD and PT entries using QEMU physical-memory reads. It decodes Present/Write/User/NX/Global/large-page flags and resolves the final guest physical address for 4 KiB, 2 MiB and 1 GiB mappings.

The **Kernel Heap** inspector reads the NativeAOT `KernelHeap` metadata directly from the stopped kernel, showing committed, allocated, free and peak bytes together with the first-fit free/live block table and allocation tokens.

The **Crash Dump Debugging** workflow writes `.nodump.json` captures beneath the OS project's `.novaoryn\crash-dumps` directory. Dumps include debugger state, registers, named locals, x64 unwind call stack, mixed disassembly, page-table translation, heap state, and stack/code memory. Exception and panic stops automatically create a dump, and a saved dump can be reopened in NovaOryn Debug without QEMU running.

## Previous release: 0.1.44

### 0.1.44 Memory viewer and named C# locals/arguments

NovaOryn IDE 0.1.44 adds a live **Memory** inspector to NovaOryn Debug. While paused, enter an absolute address or a debugger expression such as `rsp`, `rbp-0x40`, or `rsp+0x20`; the IDE reads guest virtual memory through QEMU's GDB stub and renders address, hex bytes, and ASCII in 16-byte rows. Address/size settings persist, and Memory/Watch refreshes are serialized so the QEMU GDB transport never receives overlapping requests.

The Locals panel can now consume NativeAOT **CodeView variable live ranges** from `MinimalKernel.pdb` using the bundled LLVM `llvm-pdbutil`. Supported register, register-relative, and frame-pointer-relative locations are resolved at the current RIP, so C# argument/local names are paired with their current native value and location when the PDB provides that information. If a variable has no live location at the stop, the debugger falls back to native frame slots instead of fabricating data. Named live variables can also be used by Watch/conditional expressions.

## Previous release: 0.1.42

### 0.1.42 Mixed disassembly, exception/panic breakpoints and IDE title logo

NovaOryn IDE 0.1.42 adds a mixed **C# / x64 Disassembly** view to the NovaOryn Debug inspector. Every paused stop can show the NativeAOT x64 instructions at the relocated runtime address while correlating each instruction with the nearest C# sequence point. The current instruction is highlighted and both runtime and linked-image addressing remain visible to make EFI relocation explicit.

The debugger also arms **CPU exception breakpoints before KMain** for divide error, NMI, invalid opcode, double fault, stack fault, general-protection fault, page fault and machine check. A separate **fatal/panic halt** breakpoint stops before NovaOryn enters the terminal processor halt path. Exception selections are remembered between IDE runs and can be changed while the kernel is paused.

The supplied NovaOryn cat logo is now displayed at the **top-left of the IDE title/menu bar**, using the same transparent branding asset already used by the startup screen.

Conditional/hit-count breakpoints and persistent Watch expressions from 0.1.41 remain available.

## Security audit

Run:

    Audit-NovaOrynIDE.bat

Reports are written to:

    Artifacts\Security\npm-audit-full.json
    Artifacts\Security\npm-audit-production.json

The full report includes development/build dependencies such as `@theia/cli`. The production report represents dependencies shipped with the application and is the report used by the build gate.

## Toolchain policy

The authoritative tool versions are in `Toolchain-Versions.json`.

The IDE currently pins:

- Node.js 22.22.0 LTS for the NovaOryn build host
- Python 3.13.15 for native Node module builds
- Visual Studio/MSVC x64 C++ Build Tools, including the x64 Spectre-mitigated libraries required by native dependencies
- npm supplied by the pinned Node.js distribution
- Eclipse Theia 1.74.0
- Electron 42.3.0
- `@vscode/windows-ca-certs` 0.3.4

Node.js and Python are installed under `.toolchain` so NovaOryn does not depend on arbitrary versions found on the system PATH. Visual Studio components are machine-level Microsoft software and may require elevation on first installation.

## Build

Run:

    Build-NovaOrynIDE.bat

The build automatically:

1. Verifies Windows x64.
2. Verifies/installs the pinned Node.js and Python toolchain.
3. Verifies MSVC x64 C++ support and Spectre-mitigated libraries.
4. Validates the NovaOryn/Theia/Electron dependency matrix.
5. Installs workspace dependencies without `--force` or `--legacy-peer-deps`.
6. Verifies required Theia, Windows certificate and TypeScript packages.
7. Runs the NovaOryn production dependency security gate.
8. Compiles the NovaOryn Theia extension.
9. Rebuilds Electron native modules.
10. Builds the Theia browser and Node bundles.
11. Verifies the generated frontend, backend and stylesheet outputs.

Then launch:

    Run-NovaOrynIDE.bat

`Run-NovaOrynIDE.bat` is first-run safe and invokes the build if required outputs are missing.

## Project generation

NovaOryn IDE 0.1.1 generates source from the complete configuration model. The configuration is not descriptive metadata: it is the input to the project-graph builder. For every selected facility the generator emits the appropriate component project and starter source; facilities that are not selected are not emitted.

### Architecture placement

**Monolithic:** selected drivers, storage, filesystems, networking, processes and other kernel facilities are generated under `Kernel`.

**Microkernel:** the kernel remains minimal; drivers are generated under `Drivers`, while process, filesystem and networking facilities are generated under `Services`.

**Hybrid:** drivers and low-level mechanisms remain under `Kernel`, while process, filesystem and networking facilities are generated under `Services`.

Generated projects contain `NovaOryn.json`, `NovaOryn.ProjectGraph.json`, `NovaOryn.slnx`, per-component `.csproj` files and starter C# source, `Kernel/Kernel.cs` with `KMain`, `Configuration/GeneratedConfiguration.cs`, `Build.bat`, `Run.bat`, and a README.

## Current integration boundary

The generated operating-system `Build.bat` and `Run.bat` files are connected directly to the installed NovaOryn SDK at `C:\NovaOryn`. Each generated project records that SDK root in `NovaOryn.json`, validates the corresponding SDK entry point, and forwards the generated project directory to the SDK build/run pipeline. The launchers canonicalise the operating-system directory before handing it to the SDK, avoiding a trailing-directory-separator quoting problem at the batch/PowerShell boundary. On startup, NovaOryn IDE refreshes these two generated launcher files for existing operating systems beneath `C:\NovaOrynOSes` so SDK-owned launcher fixes apply without recreating the OS.

## Repository hygiene

Generated content is not source-controlled. `.gitignore` excludes `.toolchain`, `node_modules`, compiled `lib` output, Electron distributions, security audit artifacts and npm logs.

## 0.0.18 workspace-aware dependency verification

NovaOryn IDE 0.0.18 keeps the dependency-tree and native-cache invalidation introduced in 0.0.13, but the installed-version check is now workspace-aware. The check runs with `applications/electron` as its module-resolution root, so both hoisted and workspace-local npm layouts are valid. A missing root-level `node_modules/electron` is no longer mistaken for a failed installation. The build still verifies Electron 42.3.0, `@theia/electron` 1.74.0, and the exact Electron peer requirement before auditing or compiling, then records the successful pair in `.toolchain/NovaOrynIDE-BuildState.json`.

## 0.0.18 verification fix

0.0.18 replaces the inline batch/`node -e` Theia/Electron version test with
`Verify-NovaOrynIDEInstalledDependencies.cjs`. The verifier resolves packages from
`applications/electron` with Node's workspace-aware module resolution and returns its
status directly to the build script. This avoids false dependency-mismatch failures
when the installed versions are already the required Theia 1.74.0 / Electron 42.3.0 pair.

## 0.0.18 compatibility note

NovaOryn IDE 0.0.18 keeps Eclipse Theia 1.74.0 and Electron 42.3.0, and pins `inversify` 6.2.2 directly in the NovaOryn extension. This avoids the TypeScript export-resolution failure seen through the `@theia/core/shared/inversify` re-export while preserving the same Inversify 6.x API expected by the extension. The production security gate remains enabled.

## 0.0.18 compatibility fix

NovaOryn IDE 0.0.18 explicitly pins the Lumino widget type surface used by Eclipse Theia 1.74.0 (`@lumino/widgets` 2.7.5 and `@lumino/messaging` 2.x). This prevents TypeScript workspace resolution from losing the inherited `Widget` members (`id`, `title`, and `update`) required by the NovaOryn React widget.

## 0.0.18 root Theia CLI dependency fix

NovaOryn IDE 0.0.18 makes the repository root explicitly own the Theia packages used by the root-level Theia CLI. npm workspaces are permitted to keep application dependencies inside `applications/electron/node_modules`; however, `@theia/application-manager` executes from the repository root and resolves `@theia/electron` and native Electron modules relative to its own root-level module tree. Relying on npm to hoist those packages therefore made the build layout-dependent.

The root `devDependencies` now pin the same Theia 1.74.0 / Electron 42.3.0 build surface as the Electron application. `Verify-NovaOrynIDETheiaCliDependencies.cjs` checks that the root CLI can resolve `@theia/electron`, Electron, `node-pty`, `native-keymap`, `drivelist`, and `keytar` before the build starts. These are build/runtime composition dependencies, not additional NovaOryn production features, and the production security audit remains separate.

## Run toolbar

NovaOryn IDE 0.1.30 provides a NovaOryn-owned toolbar on a dedicated row immediately below the menu/title bar. The **Run** button launches the currently opened operating system. The adjacent mode selector offers **No Debug** (Release configuration) and **Debug** (Debug configuration). The selected mode is stored in local browser storage and restored when the IDE is started again. The toolbar refreshes after workspace startup, so **Run** is enabled immediately whenever a NovaOryn OS workspace is open.

No Debug mode invokes the generated `Run.bat` with the SDK `-Run -Configuration Release` contract. Debug mode first builds the kernel with the authoritative SDK Debug pipeline, then NovaOryn IDE itself launches QEMU and owns the GDB debugging session. The SDK build process is hidden and its stdout/stderr is streamed to the **NovaOryn Build** output channel inside the IDE; it no longer opens a separate console window.

In 0.1.30, debugging is additionally gated by the operating-system configuration. **No Debug** always exports debugging as disabled. **Debug** enables only the facilities selected under the OS **Debugging** configuration (`Serial logging`, `Kernel diagnostics`, `Debug symbols`, and/or `Panic dump`). Generated `Configuration/GeneratedConfiguration.cs` exposes `DebugBuild`, `DebuggingConfigured`, `DebuggingEnabled()` and `EffectiveDebugging()` so kernel/OS code has the same effective rule at compile time. The generated SDK manifest records the configured facilities, and `Run.bat` exports `NOVAORYN_DEBUG_ENABLED`, `NOVAORYN_DEBUG_FEATURES`, plus one flag per supported facility for the SDK/runtime pipeline.

## System theme and syntax highlighting

NovaOryn IDE follows the operating system light/dark colour scheme. The Theia application default uses the built-in `light`/`dark` theme pair and NovaOryn also listens to `prefers-color-scheme` changes so the IDE updates when the system theme changes.

C# source (`.cs`) has built-in Monaco lexical syntax highlighting and language configuration without requiring the VS Code/Open VSX plugin runtime. Semantic highlighting is enabled in the frontend preferences for language services that provide semantic tokens.
