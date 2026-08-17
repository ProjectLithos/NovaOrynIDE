# NovaOryn 0.36.7

NovaOryn 0.36.7 fixes FAST-build execution of freshly compiled SDK tools.

## What failed

`Build-NovaOryn.ps1` explicitly compiled the five required FAST-build tools with `Platform="Any CPU"`. MSBuild therefore emitted them under `bin\Any CPU\<Configuration>\net10.0`. The script then resolved and executed `bin\<Configuration>\net10.0` instead. If that non-platform directory contained an older DLL, the build silently ran stale SDK code even though the log had just shown the current source compiling successfully.

This was visible with ProjectCreator 0.36.6: source contained the new bootstrap-contract verification message and the build produced `bin\Any CPU\Release\net10.0\NovaOryn.ProjectCreator.dll`, but refresh executed a stale `bin\Release\net10.0\NovaOryn.ProjectCreator.dll`, so the new migration never ran.

## Fix

FAST build now resolves ManagedCompiler, Linker, ImageBuilder, QemuLauncher, and ProjectCreator from the same `bin\Any CPU\<Configuration>\net10.0` directory into which they are compiled. It prints each resolved runtime path before invoking project refresh or managed compilation.

This removes the stale-tool ambiguity and guarantees that a successful required-tool build is followed by execution of that exact platform/configuration output.
