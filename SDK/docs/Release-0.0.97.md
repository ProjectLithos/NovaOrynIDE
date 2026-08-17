# NovaOryn 0.0.97

NovaOryn 0.0.97 repairs the incremental-source packaging failure exposed by the 0.0.96 source-policy run.

## Fixed

- `THIRD-PARTY-NOTICES.md` is revised and deliberately included in `NovaOryn-ChangedFiles-0.0.97.zip`, so applying this release restores the file even on a repository where an earlier incremental archive left it absent.
- `NovaOryn.SourcePolicy.Tests` checks `File.Exists` before reading the notice and reports a controlled policy failure instead of terminating with `FileNotFoundException`.
- `Update-NovaOryn.ps1` validates the complete target `NovaOryn-SourceManifest.json` after extraction and manifest deletions/renames. Every declared file must exist with the exact recorded byte length and SHA-256 before Git staging, commit, push, or toolchain installation.
- Source-policy coverage requires the updater's target-manifest verification to remain present.

## Unchanged

The 0.0.96 framebuffer font API repair remains unchanged. The reusable and freestanding renderers continue to use the row-based bitmap-font contract.

## Version alignment

SDK, assembly, tool, documentation, template, VSIX, image-builder, managed-compiler, QEMU-launcher, and toolchain product versions are aligned to 0.0.97.
