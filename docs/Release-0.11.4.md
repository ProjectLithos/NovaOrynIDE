# NovaOryn IDE 0.11.4

Source-layout maintenance release for root JSON consolidation.

- All JSON files formerly stored directly under the IDE root are moved to `JSON\`.
- `JSON\package.json` is the authoritative npm workspace manifest.
- npm commands use the `JSON` prefix and install into `JSON\node_modules`; a root `node_modules` junction preserves normal Node/TypeScript module resolution.
- `Security-Baseline.json`, `Toolchain-Versions.json`, and release-validation JSON files now live under `JSON\`.
- `Build-NovaOrynIDE.bat` deletes legacy root-level `*.json` files before verification.
- Location-sensitive JSON files owned by SDK, application, package, and template subtrees remain in their required locations.
