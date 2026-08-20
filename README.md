# NovaOryn IDE

**Current release: 0.11.11**

NovaOryn IDE is the desktop development environment for building, running, inspecting, testing, and debugging operating systems created with the **NovaOryn OS SDK**.

It is based on Eclipse Theia and Electron, but the product is centred on NovaOryn's own operating-system workflow: authoritative OS configuration, generated kernel/service/driver projects, an embedded SDK, NativeAOT-aware debugging, hardware and kernel inspection tools, and a freestanding .NET-oriented runtime model.

NovaOryn IDE is intended to be an operating-system development environment rather than a general-purpose code editor with a few OS commands added to it.

---

## What NovaOryn IDE provides

NovaOryn IDE currently brings the following NovaOryn-specific capabilities into one desktop application:

- creation and reconfiguration of NovaOryn operating systems;
- authoritative configuration-driven project generation;
- monolithic, microkernel, and hybrid kernel layouts;
- an embedded NovaOryn SDK and pinned build toolchain;
- Build, Run, and Debug workflows for NovaOryn OS projects;
- QEMU x64 target support and physical-machine debugger transports;
- source breakpoints before `KMain`;
- NativeAOT-aware stepping, stack unwinding, locals, arguments, memory, page tables, and heap inspection;
- exception, panic, crash-dump, and offline-dump debugging;
- Kernel Console and structured kernel-log integration;
- Target Manager;
- Driver Development Centre;
- Hardware / Device Tree;
- OS-specific static analyzers;
- Binary / Symbol Explorer;
- Memory-map Visualiser;
- Interrupt / APIC Visualiser;
- Syscall Explorer;
- Image / Disk Explorer;
- tracing, boot analysis, and performance profiling;
- SDK API documentation browser;
- Test Explorer and the NovaOryn SDK test contract;
- capability-based driver inspection and validation.

The IDE and bundled SDK are developed together so that generated OS projects, build contracts, debugging protocols, API documentation, driver contracts, and testing features remain synchronized.

---

## System requirements

NovaOryn IDE currently targets **Windows x64**.

The build system manages most of its own toolchain beneath `.toolchain` and currently pins:

- Node.js **22.22.0**;
- npm **10.9.4**;
- Python **3.13.15**;
- Eclipse Theia **1.74.0**;
- Electron **42.3.0**;
- TypeScript **5.9.x**;
- Microsoft Visual Studio / MSVC x64 C++ build tools;
- MSVC x64 Spectre-mitigated libraries.

The bundled NovaOryn SDK maintains its own .NET, NativeAOT, LLVM/LLD, NASM, QEMU, and related operating-system build dependencies.

The authoritative IDE tool versions are stored in:

```text
JSON\Toolchain-Versions.json
```

---

## Build the IDE

From the NovaOryn IDE root, run:

```bat
Build-NovaOrynIDE.bat
```

**Build means build only.** It does not launch the IDE.

The build performs the complete NovaOryn IDE validation and compilation pipeline, including:

1. source-tree and release validation;
2. embedded SDK verification;
3. pinned Node.js and Python toolchain verification;
4. MSVC/native build-tool verification;
5. Theia/Electron dependency compatibility checks;
6. npm workspace installation;
7. production dependency security checks;
8. NovaOryn feature-contract verifiers;
9. TypeScript compilation of the NovaOryn Theia extension;
10. Electron native-module rebuilds;
11. Theia browser, Node, and Electron bundle generation;
12. successful-build state recording;
13. configured source publishing to the NovaOryn IDE repository.

A successful build ends with a completed NovaOryn IDE build and produces the generated Electron application required by the Run launcher.

### Security reports

The production security gate writes reports under:

```text
Artifacts\Security\
```

The security audit can also be run directly with:

```bat
Scripts\Audit-NovaOrynIDE.bat
```

---

## Run the IDE

After a successful build, run:

```bat
Run-NovaOrynIDE.bat
```

**Run means run only.** It does not call the Build launcher.

Before starting Electron, the launcher verifies the previously built runtime, including the required Theia/Electron packages, generated backend, and matching build state. If the source has not been successfully built for the current release, Run stops and tells you to build first.

This deliberate separation keeps the two root launchers unambiguous:

```text
Build-NovaOrynIDE.bat   Build and verify NovaOryn IDE
Run-NovaOrynIDE.bat     Launch the already-built NovaOryn IDE
```

---

## Creating a NovaOryn operating system

NovaOryn IDE uses its configuration model as an **authoritative generator input** rather than as descriptive metadata.

The configuration controls areas including:

- OS name and project location;
- target architecture;
- monolithic, microkernel, or hybrid layout;
- processor and boot configuration;
- physical and virtual memory;
- scheduler and process model;
- system-call model;
- SMP and interrupt configuration;
- timers;
- drivers and driver capabilities;
- storage and filesystems;
- networking;
- input;
- graphics and audio;
- userland and shell;
- GUI selection;
- debugging;
- testing;
- virtualisation;
- RTOS and safety-oriented options.

The generated source tree is derived from those selections.

### Kernel models

**Monolithic**

Selected low-level facilities, drivers, storage, filesystems, networking, process support, and other kernel facilities are placed within the kernel-oriented project structure.

**Microkernel**

The kernel remains intentionally small. Drivers and operating-system facilities that do not need to live in the kernel are generated as separate drivers/services.

**Hybrid**

Low-level mechanisms remain kernel-side while suitable process, filesystem, networking, and service functionality can be generated outside the core kernel.

The generator creates the project graph, generated configuration, component projects, SDK bridge manifest, solution metadata, and OS build/run launchers required by the selected design.

`Kernel\Kernel.cs` is user-owned. Reconfiguration preserves it rather than regenerating the user's kernel entry source.

---

## Reconfiguring an existing OS

An existing NovaOryn OS can be reopened in the configurator from the NovaOryn menu or the Explorer root context menu.

Reconfiguration reloads the existing authoritative configuration, regenerates NovaOryn-owned files, and removes obsolete **generator-owned** component artifacts that are no longer selected.

User-owned source is preserved. In particular, the configurator does not replace `Kernel\Kernel.cs` simply because project options changed.

---

## Building and running an operating system

NovaOryn OS projects are built through the SDK bundled with the IDE.

The IDE saves modified files before an OS build/run operation and uses the generated NovaOryn project metadata to invoke the SDK pipeline for the selected operating system.

The run toolbar provides a persistent execution mode selection:

- **No Debug** — builds/runs the OS without an IDE debugger session;
- **Debug** — builds a Debug kernel and launches/attaches the NovaOryn debugger for the active target.

The selected mode is remembered between IDE runs.

The IDE streams build and launch output into its own output surfaces rather than requiring a separate command prompt for normal development.

---

## NovaOryn debugger

NovaOryn Debug is designed around operating-system and NativeAOT debugging rather than a normal managed-process debugger.

Current capabilities include:

- source breakpoints in C# kernel code;
- breakpoint relocation before `KMain`;
- Continue, Step Into, Step Over, and Step Out;
- conditional and hit-count breakpoints;
- Watch expressions;
- registers;
- CPU, thread, and process context selection;
- x64 call-stack unwinding;
- mixed C# / x64 disassembly;
- named NativeAOT locals and arguments where debug information exposes them;
- arbitrary memory inspection;
- x64 page-table inspection and address translation;
- kernel heap inspection;
- exception and panic stops;
- automatic crash-dump capture;
- offline crash-dump reopening;
- physical-machine debugger transport support.

Debugging facilities exposed to a generated operating system remain controlled by that OS's own NovaOryn configuration.

---

## Kernel panic and crash diagnostics

The NovaOryn SDK panic framework provides a formal kernel panic path with support for:

- panic reason/code;
- panic message;
- CPU context;
- thread context;
- process context;
- call stack;
- register state;
- optional crash dump;
- debugger break;
- controlled halt or reboot policy.

NovaOryn IDE integrates those diagnostics into the debugger and crash-dump views so a panic can be inspected as an operating-system failure rather than only as serial text.

---

## Test framework

The bundled SDK exposes a unified NovaOryn test contract supporting:

- kernel tests;
- unit tests;
- integration tests;
- boot tests;
- driver tests;
- stress tests;
- fault-injection tests;
- hardware-simulation tests.

The framework includes test execution/reporting contracts, timeout and fail-fast support, fault-injection facilities, hardware-simulation hooks, manifests, and machine-readable reports.

NovaOryn IDE exposes the test system through its Test Explorer and SDK integration rather than treating tests as unrelated standalone programs.

---

## Driver model

NovaOryn uses a capability-based driver model.

Drivers declare the privileged resources they need instead of assuming unrestricted hardware access. Capability areas include facilities such as:

- MMIO;
- I/O ports;
- IRQ / MSI / MSI-X;
- DMA;
- PCI configuration;
- physical memory;
- timers;
- networking;
- filesystem access.

The kernel grants those capabilities explicitly. The IDE's Driver Development Centre, static analyzers, Hardware Tree, and SDK manifests are designed around this same contract.

---

## Engineering tools

NovaOryn IDE includes engineering views intended to make low-level OS state inspectable without leaving the IDE.

### Target Manager

Defines and selects the machine/transport against which NovaOryn builds, runs, and debugs. Targets can represent QEMU, physical machines, or remote debugger endpoints where supported.

### Driver Development Centre

Surfaces driver packages, declarations, capabilities, device binding information, and related SDK contracts.

### Hardware / Device Tree

Presents the unified NovaOryn device model for PCI, USB, ACPI, platform, virtual, and logical devices in one hierarchical view.

### Static Analyzers

Applies NovaOryn-specific rules such as kernel/userland boundaries, unsafe kernel patterns, hardware-abstraction violations, interrupt allocation rules, and driver capability requirements.

### Binary / Symbol Explorer

Inspects NovaOryn binaries, PE/COFF data, sections, native symbols, debug maps, and PDB/LLVM symbol information.

### Memory-map Visualiser

Displays operating-system physical/virtual memory-map information and related region state.

### Interrupt / APIC Visualiser

Exposes interrupt-controller and interrupt-delivery state, including NovaOryn's APIC-oriented abstractions.

### Syscall Explorer

Inspects NovaOryn's shared protected syscall core and its supported syscall namespaces, including NovaOryn Get/Set/Event, Linux-style, and Windows/NT-style models.

### Image / Disk Explorer

Inspects NovaOryn disk images and storage structures, including bounded raw reads and partition/filesystem inspection supported by the current SDK tooling.

### SDK API

Provides offline access to the bundled NovaOryn SDK API documentation from inside the IDE.

---

## Structured kernel logging and telemetry

NovaOryn kernel logging supports structured records rather than plain unclassified text.

The logging contract can carry fields such as:

- severity level;
- subsystem;
- CPU;
- thread;
- process;
- timestamp;
- source.

The runtime supports multiple sinks and is designed so early boot diagnostics can operate before normal managed allocation is available.

The IDE consumes these records in its kernel-output, tracing, boot-analysis, and profiling surfaces where appropriate.

---

## Embedded SDK

NovaOryn IDE ships with the NovaOryn SDK under:

```text
SDK\
```

The embedded SDK is the operating-system build authority used by the IDE. It contains the kernel/runtime source, project creator, linker/image tooling, QEMU launcher, templates, API/ABI contracts, subsystem contracts, driver packaging model, testing contracts, documentation, and the pinned OS build toolchain.

The IDE build verifies that the bundled SDK source is present and valid before compiling the desktop application.

SDK-owned scripts and JSON files remain inside `SDK` because their paths are part of the SDK/toolchain layout and are not part of the root-directory cleanup policy.

---

## Source-tree organisation

The repository root is intentionally kept small.

```text
NovaOrynIDE\
├─ Build-NovaOrynIDE.bat
├─ Run-NovaOrynIDE.bat
├─ README.md
├─ VERSION
├─ NovaOrynIDE.ico
├─ CJS\
├─ JSON\
├─ Scripts\
├─ Ancillary\
├─ applications\
├─ packages\
├─ SDK\
├─ docs\
└─ ...
```

### `CJS`

Contains NovaOryn IDE CommonJS verifier/build-support files. Root-level `.cjs` files are not permitted.

### `JSON`

Contains IDE-root JSON configuration, release validation, npm workspace metadata, security baseline, and toolchain metadata. Root-level `.json` files are not permitted.

The npm workspace is staged under `.toolchain\NpmWorkspace` during build/run operations so the repository root does not need a `package.json`.

### `Scripts`

Contains IDE maintenance and support scripts such as auditing, dependency validation, and toolchain installation.

Only the two primary batch launchers remain at the repository root:

```text
Build-NovaOrynIDE.bat
Run-NovaOrynIDE.bat
```

### `Ancillary`

Contains ancillary text/bookkeeping files that do not belong in the repository root.

### `applications`

Contains the Electron application workspace.

### `packages`

Contains the NovaOryn Theia extension and related IDE source.

### `SDK`

Contains the embedded NovaOryn OS SDK.

---

## Root-directory hygiene

The Build launcher actively enforces the current root layout.

Legacy root-level `.cjs`, `.json`, `.txt`, and support-script files are removed or rejected according to the current source-tree rules. The two root launchers are intentionally exempt.

This allows an older extracted source tree to be upgraded without leaving obsolete copies of files beside their new `CJS`, `JSON`, `Scripts`, or `Ancillary` locations.

Generated dependencies and build products are excluded from source control through `.gitignore`.

---

## Dependency workspace

Because the repository root intentionally has no `package.json`, npm operations are performed through the staged workspace:

```text
.toolchain\NpmWorkspace\
```

Build and Run both execute their npm workspace commands from that location. They must not fall back to resolving `C:\NovaOrynIDE\package.json`.

The Run launcher validates the installed Theia/Electron runtime and generated Electron backend directly before startup.

---

## Troubleshooting

### `Could not read package.json ... C:\NovaOrynIDE\package.json`

The current release must not run npm from the repository root. Make sure the source and launchers are from the same release and run a fresh build before Run.

### Run says the IDE has not been built

Run does not build automatically. Execute:

```bat
Build-NovaOrynIDE.bat
```

and confirm it completes successfully before running:

```bat
Run-NovaOrynIDE.bat
```

### OS build/debug failure

Use the IDE's build/debug output as the primary diagnostic. NovaOryn OS compilation failures normally identify the SDK project and exact C#/NativeAOT stage that failed.

For exhaustive SDK validation independent of a normal fast OS build, use the SDK validation tooling supplied under `SDK`.

### npm security warnings

The build distinguishes the complete development dependency tree from the production dependency tree. The production security gate is authoritative for shipped dependencies, with any temporary upstream exception explicitly recorded in the security baseline.

---

## Repository and release policy

NovaOryn IDE source releases are produced as:

```text
NovaOryn-IDE-FullSource-<version>.zip
NovaOryn-IDE-ChangedFiles-<version>.zip
```

`FullSource` is the complete source tree for that release.

`ChangedFiles` contains the files required to update the preceding release and includes deletion bookkeeping where files have moved or been removed.

The source build can publish the validated source tree to the configured NovaOryn IDE Git repository after a successful build.

---

## Design direction

NovaOryn IDE is being built around a simple principle: an OS SDK should expose its architecture, contracts, hardware, memory, interrupts, system calls, drivers, tests, diagnostics, and generated structure directly to the developer.

The IDE therefore treats NovaOryn operating systems as first-class systems projects. The goal is not merely to edit C# files—it is to make the operating system itself understandable, configurable, buildable, testable, and debuggable from one environment.
