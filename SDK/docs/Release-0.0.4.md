# Nova Oryn OS SDK 0.0.4

## Fix

Corrects first-repository initialisation in `Update-NovaOryn.ps1`.

The updater now lists configured remotes before querying `origin`. When a new Git repository has no remotes, it adds:

```text
https://github.com/ProjectLithos/NovaOryn.git
```

This avoids the fatal `No such remote 'origin'` error produced under PowerShell's terminating-error policy.

## Behaviour retained

- FullSource is selected when the repository has no commit.
- ChangedFiles is selected after the first commit.
- No toolchain is downloaded.
- No commit is pushed automatically.
