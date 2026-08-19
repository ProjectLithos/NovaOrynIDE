# NovaOryn IDE 0.10.1

## Kernel panic generated-project repair

- `KernelPanicTransport.cs` is now an explicit compile item in the generated kernel project.
- The Visual Studio template manifest includes it.
- The VSIX audit requires it.
- Opening/running an existing OS refreshes all SDK-owned Boot support files and repairs an older generated `.csproj` that lacks the panic transport compile item.

## Freestanding panic ABI correction

The panic function-pointer ABI no longer accepts pointers to `KernelPanicInfo`, because that high-level record contains managed `String` references. A new `KernelPanicNativeInfo` contains only primitive/value fields for the terminal freestanding path.

`KernelPanicSnapshot` now stores only unmanaged panic context, register state, call-stack state and timestamp. Human-readable panic reason/message remain part of the public `KernelPanicInfo` and are emitted through structured telemetry rather than stored as static managed references.
