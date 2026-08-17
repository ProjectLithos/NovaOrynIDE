# NovaOryn 0.4.7

## Source-policy compile correction

0.4.6 introduced a duplicate local declaration named `kernelPhysicalBootstrap` in `tests/NovaOryn.SourcePolicy.Tests/Program.cs`. The production SDK, PMM, VMM, address-space and heap projects compiled, but the source-policy test project failed with CS0128 before the runtime path could be tested.

0.4.7 removes the duplicate declaration and reuses the existing `kernelPhysicalBootstrap` value already loaded earlier in the same policy scope. No runtime memory-management, direct-map, heap, font, Visual Studio or documentation behaviour changes are made in this release.
