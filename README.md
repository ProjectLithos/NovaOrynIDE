# NovaOryn IDE

NovaOryn IDE is the desktop development environment for the **NovaOryn Operating System SDK**. It is built on Eclipse Theia and is intended to provide a purpose-built environment for creating, configuring, building, running, and eventually debugging NovaOryn-based operating systems.

Current version: **0.0.10**

## Goals

NovaOryn IDE is designed so that operating-system configuration drives the generated source tree rather than merely changing project metadata. The long-term goal is one NovaOryn project system that can be used from the IDE, command line, and other frontends without duplicating generation logic.

The IDE currently provides the first NovaOryn project configurator and supports distinct project layouts for:

- Monolithic kernels
- Microkernels
- x86-64 targets

Generated projects use NovaOryn conventions, including the `KMain` kernel entry point and project-local build/run entry scripts.

## Repository layout

```text
NovaOrynIDE/
├── applications/
│   └── electron/                 # NovaOryn desktop/Electron application
├── packages/
│   └── novaoryn-ide/             # NovaOryn Theia extension
│       ├── src/browser/           # UI, contribution and configurator widget
│       ├── src/common/            # Shared protocol definitions
│       └── src/node/              # Backend/project generation service
├── Build-NovaOrynIDE.bat         # Toolchain verification + complete IDE build
├── Run-NovaOrynIDE.bat           # First-run-safe launcher
├── Install-NovaOrynIDEToolchain.ps1
├── Validate-NovaOrynIDEDependencies.ps1
├── Toolchain-Versions.json       # Authoritative pinned tool versions
├── package.json                  # npm workspace root
└── VERSION
```

## Toolchain policy

The NovaOryn IDE build is intended to be self-bootstrapping on Windows x64. `Build-NovaOrynIDE.bat` verifies required tools before building and installs missing prerequisites where supported.

The authoritative tool versions are defined in `Toolchain-Versions.json`. Version 0.0.10 currently pins:

- Node.js 22.22.0
- Python 3.13.15
- Eclipse Theia 1.73.0
- Electron 39.8.7
- `@vscode/windows-ca-certs` 0.3.4
- npm from the pinned Node.js distribution
- Microsoft Visual Studio / MSVC x64 C++ tooling
- MSVC x64/x86 Spectre-mitigated runtime libraries required by native Node modules

Node.js and Python are installed under the repository-local `.toolchain` directory so builds do not depend on arbitrary versions found on the system `PATH`.

Visual Studio/MSVC remains machine-level software. The bootstrap detects a suitable existing Visual Studio installation and can request required C++ components through the Visual Studio Installer.

## Building

From a Windows command prompt in the repository root, run:

```bat
Build-NovaOrynIDE.bat
```

The build performs the following high-level steps:

1. Verifies Windows x64.
2. Reads the pinned toolchain versions.
3. Verifies or installs Node.js and Python.
4. Verifies MSVC x64 C++ support.
5. Verifies the required Spectre-mitigated MSVC libraries.
6. Validates the pinned Theia/Electron dependency matrix.
7. Installs npm workspace dependencies, including development dependencies.
8. Verifies the Theia CLI and Windows CA certificate native module.
9. Compiles the NovaOryn Theia extension.
10. Rebuilds Electron native modules.
11. Builds the Theia browser and Node bundles.
12. Verifies the generated Electron backend before reporting success.

The build intentionally does **not** use `npm --force` or `--legacy-peer-deps` to hide dependency conflicts.

## Running

After a successful build, run:

```bat
Run-NovaOrynIDE.bat
```

`Run-NovaOrynIDE.bat` is first-run safe: if required dependencies or compiled output are missing, it invokes the build process before starting the IDE.

## Generated operating-system layouts

### Monolithic

A monolithic project places major operating-system facilities beneath the kernel tree, including drivers, filesystems, networking, processes, scheduling, interrupts, and memory management.

### Microkernel

A microkernel project keeps the kernel tree smaller and generates major services outside the kernel, including device management, filesystem, networking, and process management services.

## Current development status

NovaOryn IDE is in very early development. Version 0.0.10 establishes the custom Theia/Electron application, NovaOryn project configurator, project generator, reproducible toolchain bootstrap, and strict dependency/build validation.

The generated operating-system `Build.bat` and `Run.bat` files are currently integration entry points. Direct integration with the main NovaOryn NativeAOT/ILC/LLVM/LLD/QEMU build pipeline is planned for a later release.

## Build notes

The IDE uses strict TypeScript checking. The NovaOryn extension is compiled with TypeScript and then composed into the Electron application by Theia.

The Electron application pins dependencies required by the Windows Node bundle explicitly where necessary. In particular, `@vscode/windows-ca-certs` is a direct dependency because Theia's Node bundle reaches it through `@vscode/proxy-agent` on Windows.

## Contributing

NovaOryn IDE is under active development. When making changes:

- keep dependency versions pinned and internally consistent;
- do not bypass npm dependency errors with `--force` or `--legacy-peer-deps`;
- preserve strict TypeScript compilation;
- keep generated project architecture driven by the selected NovaOryn configuration;
- keep build-tool discovery and installation reproducible;
- avoid committing `node_modules`, `.toolchain`, generated `lib` output, or build logs.

## Project

NovaOryn IDE is part of the NovaOryn Operating System SDK project.
