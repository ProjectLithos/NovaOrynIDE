# NovaOryn IDE 0.1.39

Fixes NativeAOT source-debug map schema compatibility. The IDE now accepts both PascalCase and camelCase source-map entry properties, validates entries before path resolution, and the bundled SDK emits camelCase source maps for future Debug builds. This removes the `paths[0]` undefined crash immediately after EFI relocation.
