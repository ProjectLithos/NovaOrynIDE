# NovaOryn 0.0.56

## Summary

Release 0.0.56 corrects the architecture-contract XML documentation failure and synchronises the internal Visual Studio project-template version with the installed VSIX version.

## Changes

- Added complete XML documentation for `ICpu` and every public member.
- Added the missing `ICpu.cs` file to the FullSource release tree.
- Updated `NovaOrynKernel.vstemplate` from the stale `NovaOryn Kernel 0.0.41` name to `NovaOryn Kernel 0.0.56`.
- Added VSIX build validation that rejects a template whose internal name does not match the VSIX version.
- Updated SDK, VSIX, template, assembly, toolchain and runtime metadata to 0.0.56.
