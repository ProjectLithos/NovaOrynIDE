# NovaOryn 0.0.70

NovaOryn 0.0.70 corrects a source-policy test compilation failure introduced in 0.0.69.

The test program declared the local variable `projectCreator` twice in the same top-level scope. The later declaration is now named `projectCreatorDefaults`, preserving both checks while allowing `NovaOryn.SourcePolicy.Tests` to compile.

This release does not change the kernel architecture or reintroduce low-level implementation details into the end-user `Kernel.cs`.
