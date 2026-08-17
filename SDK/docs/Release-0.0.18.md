# Nova Oryn OS SDK 0.0.18

Version 0.0.18 prevents the NativeAOT diagnostic link from appearing to hang.

## Changes

- replaces the unbounded `/errorlimit:0` link with bounded `/errorlimit:256` passes
- reads LLD stdout and stderr concurrently, preventing redirected-pipe deadlock
- terminates an LLD pass after two minutes instead of leaving the build indefinitely blocked
- discovers and stubs retained stock-runtime host imports incrementally
- limits the compatibility process to 32 deterministic passes
- preserves immediate failure for unresolved NovaOryn and NativeAOT runtime contracts

This remains an interim compatibility stage while the freestanding NovaOryn CoreLib and runtime pack are developed.
