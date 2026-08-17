# Nova Oryn OS SDK 0.0.6

## Purpose

This corrective release fixes first-commit detection in `Update-NovaOryn.ps1`.

## Correction

The updater no longer invokes `git rev-parse --verify HEAD` directly through PowerShell when the repository has no commits. That direct invocation could be promoted to a terminating native-command error and print:

```text
fatal: Needed a single revision
```

The updater now runs the quiet `HEAD` check through `Start-Process`, inspects only the Git exit code, and suppresses the expected diagnostic for an empty repository.

## Behaviour

- no `.git` directory: select the latest FullSource archive
- `.git` exists but `HEAD` does not: still select the latest FullSource archive
- `HEAD` exists: select the latest ChangedFiles archive
- no toolchain is downloaded
- no push is performed automatically
