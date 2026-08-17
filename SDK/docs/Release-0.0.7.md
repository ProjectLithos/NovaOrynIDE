# Nova Oryn OS SDK 0.0.7

## Purpose

Version 0.0.7 corrects the incremental update workflow when a ChangedFiles archive has already been extracted into `C:\NovaOryn`.

## Behaviour

The updater now accepts an existing dirty working tree only when every changed, deleted, renamed, or untracked path is supplied by the selected ChangedFiles archive. Unrelated local edits still stop the update.

The accepted release changes are staged, committed, pushed to `origin/main`, and only then may the required toolchain be downloaded.
