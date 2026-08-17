# Nova Oryn OS SDK 0.0.44

This maintenance release corrects two host-tool integration faults found after 0.0.43.

- The documentation build now discovers the generated executable beneath platform-specific output paths such as `bin\x64\Release\net10.0`.
- The VSIX build deletes stale `bin` and `obj` output before packaging.
- The built VSIX manifest is verified to contain extension identity `NovaOryn.VisualStudio` version `0.0.44`.
- The VSIX installer now removes an older NovaOryn extension before installing the newly built package.

Run `Build-NovaOrynDocumentation.bat`, then `Install-NovaOrynVSIX.bat`.
