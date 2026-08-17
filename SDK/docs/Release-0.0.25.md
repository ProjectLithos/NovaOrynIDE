# Nova Oryn OS SDK 0.0.25

## Purpose

This release fixes the compilation-manifest schema mismatch discovered after the first successful direct ILC compilation of the no-CoreLib bootstrap kernel.

## Confirmed progress

The Windows build log proved that:

- Roslyn produced `NovaOryn.Kernel.Bootstrap.dll`;
- the repository-pinned `ilc.exe` produced `MinimalKernel.obj`;
- no Windows CoreLib or Windows NativeAOT runtime library was linked;
- the build reached `NovaOryn.Linker.exe`.

## Failure corrected

`NovaOryn.ManagedCompiler.exe` wrote compilation manifest schema 5, while `NovaOryn.Linker.exe` still accepted only schema 4. The linker therefore stopped with:

```text
[FAIL] Unsupported compilation manifest schema: 5
```

Version 0.0.25 defines schema 5 as the linker's supported compilation-manifest schema. The error message now reports both the received schema and the supported schema.

## Regression protection

Source-policy tests now require:

- the managed compiler to emit `schemaVersion = 5`;
- the linker to declare `SupportedCompilationManifestSchema = 5`;
- the linker to continue consuming the direct ILC `nativeObject`;
- the linker not to regress to the obsolete NativeAOT static-library model.

## Expected next build stage

The next build should proceed beyond manifest validation to:

```text
llvm-nm export validation
    -> LLD link
    -> MinimalKernel.efi
```
