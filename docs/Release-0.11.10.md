# NovaOryn IDE 0.11.10

NovaOryn IDE 0.11.10 reorganizes root-level support files.

- `Build-NovaOrynIDE.bat` and `Run-NovaOrynIDE.bat` remain the only root scripts.
- IDE audit, dependency-validation and toolchain-bootstrap scripts move to `Scripts\`.
- root `.txt` package bookkeeping moves to `Ancillary\`.
- the build removes legacy root text files and root scripts other than the two launchers.
- SDK-owned scripts remain in `SDK\` because their locations are part of the SDK build/toolchain contract.
