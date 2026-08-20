# NovaOryn IDE 0.11.2

## CJS source organisation

- Moves every `.cjs` file into the root-level `CJS` directory.
- Updates IDE build verifier paths to use `CJS\...`.
- Moves the extension asset helper to `CJS/extension-files.cjs` and updates the package scripts.
- Preserves verifier behavior by resolving project-root paths from the new directory.
- Bumps active NovaOryn IDE release metadata to 0.11.2.
