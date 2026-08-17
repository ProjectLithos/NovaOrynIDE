# Nova Oryn OS SDK 0.0.8

## Purpose

Version 0.0.8 corrects cumulative pre-extracted update handling.

When earlier ChangedFiles releases were extracted but not committed, their files remained dirty alongside the newest release. The updater previously trusted only paths in the newest archive and therefore rejected genuine NovaOryn files from earlier releases.

## Correction

The updater now accepts a dirty file only when either:

- its exact SHA-256 matches the selected ChangedFiles archive, or
- its exact SHA-256 matches an entry in the already supplied `NovaOryn-SourceManifest.json`.

Deleted or renamed paths remain accepted only when the selected release explicitly declares them. Any unrelated or modified local file is still rejected.

After validation, the updater extracts the newest release, commits the complete accumulated NovaOryn source changes, pushes `main`, and only then installs missing toolchain components.
