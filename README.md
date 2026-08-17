NovaOryn IDE 0.1.40 bundles NovaOryn SDK 0.37.4 and uses a QEMU debugcon relocation rendezvous so exact C# breakpoints are armed before KMain without relying on guest INT3 handling.

# NovaOryn IDE

### 0.1.30 Existing OS reconfiguration

NovaOryn IDE 0.1.30 adds an authoritative **Reconfigure OS** workflow for an operating system that is already open. Use either the new **NovaOryn > Reconfigure OS** menu command or right-click the operating-system root folder in Explorer and choose **Reconfigure NovaOryn OS**.

The configurator reloads the current `NovaOryn.json` selections instead of starting from defaults. Applying the configuration rebuilds `NovaOryn.ProjectGraph.json`, generated component projects, `Configuration/GeneratedConfiguration.cs`, `NovaOryn.slnx`, the SDK bridge manifest, and the generated Build/Run launchers. Generated components that are no longer selected have only their generator-owned `.csproj` and `GeneratedFeature.cs` artifacts removed; directories containing user files are retained.

`Kernel\Kernel.cs` is explicitly user-owned during reconfiguration and is never regenerated or replaced. The OS name and root path are also fixed during reconfiguration so changing kernel architecture, drivers, services, filesystems, networking, userland, GUI, testing or safety options cannot accidentally turn the operation into a project rename.

The Run/Debug toolbar fixes from 0.1.12 remain in place: the toolbar stays below the menu, uses the standard Theia `ApplicationShell`, saves dirty editors before build/run, and streams SDK output inside the IDE.


NovaOryn IDE is the desktop development environment for the NovaOryn Operating System SDK. It is a custom Eclipse Theia desktop application with NovaOryn-specific project configuration and generation.

## Current release: 0.1.40

### 0.1.40 Source stepping and debug inspection

NovaOryn IDE 0.1.40 turns the previously machine-step-only controls into source-oriented Debug operations. **Step Into** machine-steps until the NativeAOT source line changes. **Step Over** recognizes x64 CALL instructions and runs to a temporary return-site breakpoint so ordinary calls are skipped while user breakpoints remain authoritative. **Step Out** uses the current x64 frame return address and continues to the caller. Continue, Pause, Restart and Stop remain connected directly to the QEMU GDB stub.

When execution is paused, the IDE now paints a current-statement arrow and whole-line highlight, automatically reveals the stopped C# line, and opens a **NovaOryn Debug** inspector containing the call stack, native frame/stack values and x64 integer registers. Call-stack rows with source information can be clicked to open that frame. Because the current SDK source-map JSON does not yet export named NativeAOT C# local-variable records, the Locals view deliberately exposes native frame/argument slots rather than inventing C# variable names.

### 0.1.39 breakpoint fixes retained

The 0.1.39 debug-map schema compatibility and verified source-breakpoint binding fixes remain in 0.1.40. The IDE accepts both PascalCase and camelCase source-map entries, binds non-executable C# lines to nearby executable sequence points, and holds the kernel before KMain if a requested source breakpoint cannot be verified.



### 0.1.32 Bundled SDK toolchain mode

NovaOryn IDE 0.1.36 bundles NovaOryn SDK 0.37.3 under `SDK\` and verifies its toolchain in embedded mode. The embedded SDK no longer has to be a separate Git repository or clean standalone checkout. Standalone SDK installation still keeps the normal Git repository/clean-tree safety gate.

### 0.1.30 Debug build failure propagation

NovaOryn IDE 0.1.30 fixes generated `Run.bat` Debug error propagation. The batch file now uses delayed expansion for `ERRORLEVEL`, so an SDK Debug build failure is returned to the IDE immediately instead of being mistaken for success and followed by misleading missing `NovaOryn.DebugSymbols.json` errors. It is paired with NovaOryn SDK 0.36.9 for the corrected EFI debug-anchor link calculation.

### 0.1.30 Theia-native breakpoint UI

NovaOryn no longer reimplements Monaco breakpoint gutter handling or breakpoint decorations. The Electron application now ships Eclipse Theia's native `@theia/debug` package, whose `DebugEditorModel` owns the glyph-margin click handler, persistent source breakpoints, F9, hover hints and breakpoint glyph rendering. This is the same editor/debug integration used by Theia itself.

NovaOryn's **Debug -> Toggle Breakpoint** source-editor context command is retained and is bridged to the same Theia `BreakpointManager`, so the gutter, F9, toolbar and NovaOryn context menu all operate on one authoritative source-breakpoint collection. Native Theia breakpoint changes are mirrored into the NovaOryn/QEMU GDB session, and all stored Theia breakpoints are armed when Debug starts.

At frontend startup the NovaOryn Build output records `Breakpoint UI ready` and explicitly identifies Theia's native breakpoint subsystem.

### Exact C# source breakpoints (from 0.1.23)

Debug mode consumes the NovaOryn SDK 0.36.9 native source-debug manifest rather than guessing a C# breakpoint from the containing method name in the linker map. QEMU is internally held only long enough for the debugger to attach and arm early source breakpoints, then execution immediately continues until a requested breakpoint is reached or Pause is pressed.


### 0.1.1 workspace startup

NovaOryn IDE now treats `C:\NovaOrynOSes\` as the authoritative operating-system root. At startup the main NovaOryn page displays the branded artwork, scans immediate subfolders for `NovaOryn.json`, and offers each existing OS for opening. `Create New OS` opens the authoritative configuration pages; generated systems are always created as `C:\NovaOrynOSes\<OS name>` regardless of stale location metadata.


0.1.1 makes the NovaOryn operating-system configuration authoritative. The configurator now covers the kernel model, CPU and boot architecture, memory, scheduling, processes, system calls, SMP, interrupts, timers, drivers, storage, filesystems, networking, input, graphics, audio, userland, shell, GUI, diagnostics, tests, virtualisation and RTOS/safety policy.

### Authoritative project generation

- `NovaOryn.json` uses schema version 2 and records every generation selection.
- `NovaOryn.ProjectGraph.json` records the concrete generated component graph.
- Monolithic, microkernel and hybrid selections now change where projects are generated rather than merely changing metadata.
- Disabled subsystems are omitted from the generated source tree.
- Selected drivers, storage controllers, timers, network adapters, graphics, input, debugging and test programs each generate their own component project and starter source.
- Microkernel generation places drivers outside the kernel and service-oriented facilities under `Services`.
- Monolithic generation places kernel-facing facilities under `Kernel`.
- Hybrid generation keeps drivers/kernel mechanisms in the kernel while process/filesystem/network facilities are generated as services.
- A generated `NovaOryn.slnx` and per-component `.csproj` files expose the configured project graph to the IDE/tooling.
- `Configuration/GeneratedConfiguration.cs` makes the selected configuration available to generated C# source.
- Build and run entry points remain connected to the NovaOryn SDK at `C:\NovaOryn`.

### Startup splash screen

The animated branded splash screen from 0.0.21 remains unchanged in 0.1.1.

### Security changes

- All Eclipse Theia packages are pinned coherently to 1.74.0.
- Electron is pinned to 42.3.0, the exact peer required by `@theia/electron` 1.74.0.
- VS Code/Open VSX runtime plugin loading is temporarily removed from the shipped application. This keeps the known critical `decompress` archive-extraction dependency out of the production dependency tree. It can be restored when the upstream chain has a safe compatible resolution.
- `Audit-NovaOrynIDE.bat` generates both a full dependency audit and a production-only audit.
- The normal build runs the production security gate after dependency installation.
- Any production critical vulnerability fails the build.
- Any production high vulnerability fails the build unless it appears in `Security-Baseline.json` with an explicit temporary upstream exception and review date.
- The full development dependency audit is still retained because build-only vulnerabilities matter, but build-time findings are not confused with vulnerabilities shipped in the desktop runtime.
- NovaOryn never runs `npm audit fix --force` automatically.

The temporary production-high exceptions in 0.0.18 are limited to Electron and its directly inherited packaging chain because Theia 1.74.0 requires Electron 42.3.0 exactly. They are recorded in `Security-Baseline.json` and must be reviewed after 2026-08-31 rather than silently accepted forever.

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
