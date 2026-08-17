# NovaOryn 0.2.1

NovaOryn 0.2.1 is a corrective release for the 0.2.0 virtual-memory integration.

## Corrections

- Visual Studio ignores a second F5/Ctrl+F5 NovaOryn launch while one NovaOryn operation is already active. This prevents concurrent build pipelines from refreshing the same generated `Sdk` tree.
- `NovaOryn.ProjectCreator` no longer deletes the live `Sdk` directory during normal refresh. SDK-owned files are refreshed in place so Visual Studio/MSBuild project handles do not race recursive directory deletion. Legacy `Boot`, `Console`, and `Runtime` generated trees are still removed.
- A narrowly targeted migration rewrites the generated 0.2.0 virtual-memory statistics interpolation fragment when it is found in an existing user kernel. The replacement is compatible with the freestanding CoreLib, which intentionally does not provide `string.Format`.
- Source-policy tests require both kernel templates to remain free of interpolated strings/`string.Format` and require the VSIX launch serialization and safe project refresh behaviour.

## Scope

No physical-memory or virtual-memory mapping semantics changed in this release. The 0.2.0 VMM API and x64 paging implementation remain intact.
