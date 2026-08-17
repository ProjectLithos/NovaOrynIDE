# Nova Oryn OS SDK 0.0.3

## Purpose

This release adds the source archive and Git commit updater.

## Added

- `Update-NovaOryn.bat`
- semantic-version selection of the latest source archive
- FullSource selection for a repository with no commits
- ChangedFiles selection after the initial commit
- automatic Git repository initialisation and origin validation
- staged deletion and rename support through `NovaOryn-Changes.json`
- protection against overwriting uncommitted work

## Deliberate boundaries

The updater does not:

- push to GitHub
- download or update the toolchain
- build, link, package, or run a kernel
- create source code

After the commit is reviewed and pushed, the separate NovaOryn setup executable may download the pinned toolchain.
