# Nova Oryn OS SDK 0.0.43

## Documentation generator build correction

Release 0.0.43 corrects the documentation generator entry point introduced in 0.0.42.

`Program.cs` now imports the `NovaOryn.DocumentationGenerator` namespace before its top-level statements. The previous file-scoped namespace declaration could not legally precede top-level statements and caused CS1026, CS0116, CS1022 and CS8803 compiler errors.

The Visual Studio extension version is also advanced to 0.0.43 so the VSIX installer recognises this package as an update rather than reporting that the same extension version is already installed.
