# NovaOryn IDE 0.14.10

Corrects the npm dependency reuse decision that could classify a partial `node_modules` tree as complete when `electron` and `@theia/electron` existed but required development packages such as `@theia/cli` were missing.

Before reusing an installed npm tree, the build now runs `Scripts/Validate-NovaOrynIDERuntimePackages.ps1`. The verifier derives the required external package set from the root, Electron application and NovaOryn extension manifests. Missing packages make the tree incomplete and trigger one clean npm installation. The newly installed tree is verified again before the build proceeds.

The dependency-state manager also refuses to stamp a verified dependency marker unless Electron, `@theia/electron`, `@theia/cli`, and `@vscode/windows-ca-certs` are present at the pinned versions. This prevents a partial tree from being remembered as valid.
