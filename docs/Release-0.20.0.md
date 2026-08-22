# NovaOryn IDE 0.20.0

0.20.0 introduces the NovaOryn package-manager format. NovaOryn packages deliberately use the standard `.zip` container and are recognised by a required root `NovaOryn.Package.json` manifest using `novaoryn-package-v1` schema version 1.

A single package contract supports applications, drivers, libraries, services, and kernel extensions. The package verifier checks ZIP entry safety, duplicate names, declared payload ownership, lengths and SHA-256 hashes. Package class rules preserve the 0.19.x `.exe`/`.nexe` application format and the existing `.nodrv` driver artifact.

The SDK includes `NovaOryn.PackageFormat`, `NovaOryn.PackagePacker`, and `NovaOryn.PackageManager`. The manager provides verify/inspect/install/uninstall/list operations, required dependency checks, reverse dependency protection, transaction staging, same-volume atomic install moves, and an atomic installed-package database. Kernel extensions require a signed or trusted package policy; capability declarations remain requests rather than grants.
