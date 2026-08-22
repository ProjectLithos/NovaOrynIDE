# NovaOryn executable/application format

NovaOryn 0.19.0 defines a stable application package contract while keeping end-user filenames familiar.

## Default visible extensions

- **`.exe`** — packaged NovaOryn application. This is a NovaOryn package container identified by its `NOAP` magic; the extension alone never makes data executable.
- **`.nexe`** — architecture-specific native executable image stored inside a package. x64 uses the existing validated PE32+/ELF64 image loader; PE32+ is the preferred NativeAOT/LLD payload.
- **`.dll`** — dynamic/shared userland library.
- **`.lib`** — static/import development library.

An OS may override file associations for presentation, but the package ABI, magic and loader validation are unchanged.

## Package header

The canonical binary package starts with a 192-byte little-endian header (`NOAP`, format 1.0) containing architecture, syscall ABI, ABI major/minor, flags, package size, native-image range, entry-point RVA, dependency/capability/resource tables, a UTF-8 string table, resource-data range, identity, name, version, publisher and minimum-SDK references. All ranges are bounds checked before use.

## Metadata

A package formally identifies: application ID, display name, application version, publisher, architecture (`x86_64`, `arm64`, `riscv64`), NovaOryn ABI major/minor, syscall personality (`novaoryn`, `linux`, `windows-nt`), native image, semantic entry-point RVA, dependencies, required capabilities and resources.

The application version is independent of the NovaOryn ABI version. Major ABI mismatch is rejected; a package requiring a newer minor ABI is rejected.

## Dependencies

Dependencies are explicit ID + version-constraint records. Runtime loaders/package managers must resolve declared dependencies rather than performing unrestricted directory searches. This is the basis for deterministic loading and prevents path-preloading from becoming part of the NovaOryn ABI.

## Capabilities

The package declares capabilities it **may request**. Declaration is not authority. NovaOryn security policy decides which capability handles are actually granted to the process. This directly composes with the 0.18.0 process-scoped capability/handle model.

## Resources

Resources are immutable package records with a name, offset, length and flags. Package resources are read-only by default; mutable application data belongs in the user's application-data area rather than in the installed `.exe`.

## Entry point and security

The package stores an entry-point RVA, never a fixed virtual address. The process loader unwraps the `.nexe`, validates the native executable, verifies the package RVA against the image entry point when supplied, then creates the private user address space. Existing W^X, NX, guard-page, privilege-ring, syscall and user-pointer validation remain authoritative.

## Packaging tool

`NovaOryn.ApplicationPacker` consumes `NovaOryn.Application.json` and emits the canonical `.exe` binary package. The manifest supports identity/version/publisher, architecture, ABI, syscall ABI, entry-point RVA, native image, dependency records, requested capability records and resource files.

Example:

```json
{
  "id": "com.example.editor",
  "name": "Editor",
  "version": "1.0.0",
  "publisher": "Example",
  "architecture": "x86_64",
  "syscallAbi": "novaoryn",
  "abiMajor": 1,
  "abiMinor": 0,
  "entryPointRva": 4096,
  "nativeImage": "bin/x64/Editor.nexe",
  "dependencies": [{ "id": "NovaOryn.UI", "version": "1.x" }],
  "requiredCapabilities": [{ "name": "graphics.window", "rights": 65 }],
  "resources": [{ "name": "icon", "path": "resources/icon.png", "flags": 1 }]
}
```
