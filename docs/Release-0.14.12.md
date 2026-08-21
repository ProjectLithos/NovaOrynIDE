# NovaOryn IDE 0.14.12

Corrects the TypeScript and React declaration dependency checks after the npm dependency tree has already passed NovaOryn's authoritative direct-manifest validation.

In 0.14.11 the build correctly verified all required manifests, including the extension development dependencies, then performed separate workspace-scoped `npm ls` checks for `typescript` and `@types/react`. npm can report `(empty)` for a workspace-scoped depth-zero query when those packages are hoisted to the staged workspace root, causing a false build failure even though the packages are installed.

0.14.12 removes those contradictory workspace-scoped `npm ls` gates. `Scripts/Manage-NovaOrynIDEBuildState.ps1 -Action VerifyDependencies` now directly requires the installed `typescript/package.json` and `@types/react/package.json` alongside Electron, Theia CLI and Windows CA. `Scripts/Validate-NovaOrynIDERuntimePackages.ps1` remains the complete manifest-driven dependency-set verifier used before reuse/install decisions.

The build therefore has one installation truth source: direct installed package manifests and declared versions/ranges. Hoisting no longer causes TypeScript, React declarations or Windows CA to be falsely reported missing.
