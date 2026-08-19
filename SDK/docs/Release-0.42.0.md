# NovaOryn SDK 0.42.0 — Driver packaging

This release completes the professional driver-packaging layer after the 0.39 lifecycle work.

- Defines the authoritative `NovaOryn.Driver.json` schema v3.
- Requires stable package ID, driver/device IDs, architecture, minimum NovaOryn version, SDK API version, Driver ABI version, dependencies, capabilities/permissions, and signing metadata.
- Adds build-time manifest validation with architecture/API/ABI/minimum-version compatibility checks.
- Rejects revoked packages and requires metadata for signed/trusted package states.
- Adds `.nodrv` packaging through `Pack-NovaOrynDriver`.
- Updates NovaOryn IDE-generated drivers and Visual Studio driver templates to create package manifests by default.

Signing metadata is now part of the stable package format. Cryptographic trust-chain enforcement remains a future policy layer rather than being faked by this release.
