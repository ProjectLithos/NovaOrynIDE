# NovaOryn 0.25.4

0.25.4 repairs an incremental-release integrity mismatch in the PS/2 input stack.

## Correction

- Reissues the authoritative `NovaOryn.Kernel.Ps2` source together with bootstrap and both generated-kernel templates.
- Adds `KernelPs2.InputContractVersion = 2` for the decoded keyboard-event contract.
- Keeps `KernelPs2.SetKeyboardEventHandler(...)` as the decoded event registration point.
- Bootstrap and templates verify input contract version 2 before registering `HandleKeyboardEvent`.
- Build policy now requires the contract version and handler registration API, preventing a future bootstrap/PS2 source mismatch.

## Reported failure repaired

An incremental 0.25.3 installation could compile `NovaOryn.Kernel.Ps2` successfully but then fail bootstrap compilation with CS0117 because the installed PS/2 source was older than the bootstrap source and did not contain `SetKeyboardEventHandler`. FullSource 0.25.3 already contained the member; this release deliberately changes and reissues the coupled files so ChangedFiles repairs an older incremental tree as well.
