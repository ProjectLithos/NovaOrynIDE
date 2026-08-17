# NovaOryn 0.0.54

## Architecture contracts dependency fix

Release 0.0.54 corrects the build dependency for `NovaOryn.Architecture.Contracts`.

`ICpu.cs` uses `NovaOryn.Primitives.ProcessorId`, but the architecture-contracts project referenced only `NovaOryn.Core`. The project now contains an explicit project reference to `NovaOryn.Primitives`, allowing the compiler to resolve both the namespace and `ProcessorId` type.

This release also documents that the supplied 0.0.53 FullSource archive did not contain every file present in the installed repository. The ChangedFiles package is therefore the authoritative patch for an existing `C:\NovaOryn` tree.
