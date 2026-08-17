# NovaOryn 0.0.20

## Freestanding NativeAOT bootstrap runtime

This release replaces the Windows NativeAOT runtime-link experiment with a NovaOryn-owned no-GC bootstrap system module.

The bootstrap supports only the first proof target:

- x64 UEFI application
- user-owned `bool KMain(BootContext)`
- primitive value types
- direct native imports for serial port I/O and CPU halt
- NativeAOT transition helpers required for P/Invoke and reverse P/Invoke
- no Windows CoreLib
- no Windows runtime libraries
- no automatic unresolved-symbol stubs

Not yet available in this runtime stage:

- managed heap or garbage collection
- exceptions
- arrays created at runtime
- general strings and formatting
- threading
- reflection
- finalization
- stack walking

These features will be introduced deliberately in later runtime-pack stages.
