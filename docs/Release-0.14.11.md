# NovaOryn IDE 0.14.11

Corrects the Windows CA certificate dependency verification after the npm dependency tree has already passed NovaOryn's authoritative direct-manifest validation.

In 0.14.10, `Scripts/Manage-NovaOrynIDEBuildState.ps1 -Action VerifyDependencies` correctly located and version-checked the hoisted `node_modules/@vscode/windows-ca-certs/package.json`, then `Build-NovaOrynIDE.bat` performed a second, contradictory check using `npm ls @vscode/windows-ca-certs --workspace @novaoryn/ide-electron --depth=0`. npm can report `(empty)` for that workspace-scoped depth-zero query when the dependency is hoisted to the workspace root, causing a false build failure even though the package is installed and correctly pinned.

0.14.11 removes the unreliable workspace-scoped `npm ls` gate. The authoritative dependency verifier is now the single source of truth for Electron, `@theia/electron`, `@theia/cli`, and `@vscode/windows-ca-certs`. The build reports the Windows CA check as already satisfied by that direct manifest/version verification and continues to the remaining Theia dependency, security, TypeScript and production-build stages.

The 0.14.11 release verifier rejects any reintroduction of the contradictory `npm ls --workspace` Windows CA check and requires the direct Windows CA manifest/version validation to remain present.
