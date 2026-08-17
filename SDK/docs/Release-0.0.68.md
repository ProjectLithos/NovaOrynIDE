# NovaOryn 0.0.68

NovaOryn 0.0.68 corrects a malformed source-policy regression assertion introduced in 0.0.67.

## Corrected policy-test source

The test intended to verify that `NovaOryn.ManagedCompiler` discovers every managed bootstrap DLL with `GetFiles(managedOutput, "*.dll")`. The `*.dll` quotes were not escaped inside the test's own C# string literal, causing `NovaOryn.SourcePolicy.Tests` to fail compilation with CS1525 and CS1003 before the policy suite could run.

This release:

- escapes the embedded `*.dll` quotes correctly;
- retains validation of `managedInputs` and `systemModuleAssembly`;
- keeps the complete multi-assembly direct ILC input introduced in 0.0.67;
- changes no kernel, console, platform, low-level, native, or linker behaviour.
