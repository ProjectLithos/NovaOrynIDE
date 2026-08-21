# NovaOryn IDE 0.14.8

Corrects the dependency/build control flow that still terminated 0.14.7 immediately after a successful Theia/Electron verification.

Dependency state is now separate from release build state in both behaviour and validity. The npm dependency fingerprint is derived from dependency declarations rather than the NovaOryn IDE application version, so a normal version bump no longer forces all npm packages to be reinstalled when the dependency graph has not changed.

Installed Electron and `@theia/electron` validation now runs through the build-state manager's `VerifyDependencies` action, which validates the actual staged packages and records the dependency-state marker in one operation. The completed-build state remains version-specific and is written only after the full IDE build succeeds.
