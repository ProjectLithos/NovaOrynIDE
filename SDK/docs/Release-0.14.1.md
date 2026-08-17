# NovaOryn 0.14.1

NovaOryn 0.14.1 is a corrective release for roadmap item 21, networking.

## Corrected template-policy test scope

The networking root-project reference assertion in `NovaOryn.TemplatePolicy.Tests` was emitted immediately after the storage `foreach` loop instead of inside its own loop. The assertion therefore referenced the loop variable `project` outside its scope and produced CS0103. The networking assertion now has its own explicit loop over the command-line and Visual Studio root projects.

## Corrected networking host-test project

`NovaOryn.Networking.Tests` incorrectly referenced the freestanding `NovaOryn.Kernel.Networking` project directly. That project intentionally disables implicit framework references and uses NovaOryn's freestanding CoreLib, so those project settings propagated into the host-side test build and removed normal `System.Void`, `System.String`, `System.Object`, and framework attribute types.

The networking test now follows the same host-test model already used by the driver and storage tests: it links the testable networking contract, math, and DHCP/DNS helper source files together with the driver contract source while compiling as an ordinary `net10.0` executable.

No networking-kernel runtime semantics changed in this corrective release.
