# Nova Oryn OS SDK 0.0.21

## Purpose

This release corrects the first compilation failure in the NovaOryn-owned no-standard-library NativeAOT bootstrap project.

## Corrections

- disables SDK-generated implicit global usings for `NovaOryn.Kernel.Bootstrap`
- disables nullable metadata generation for the custom system module
- disables implicit framework references in addition to `NoStdLib`
- removes `PublishAot`, `SelfContained`, and `RuntimeIdentifier` from the ordinary `NovaOryn.Kernel.Sample` solution build
- keeps architecture selection inside the dedicated NovaOryn ILC publish stage
- documents why the Microsoft ILC package is currently selected through the `win-x64` runtime identifier

## About `win-x64`

`win-x64` is currently the package-selection identifier used by the installed Microsoft .NET SDK to locate its x64 NativeAOT compiler assets. It describes the compiler/runtime-pack package being consumed on the Windows build host. It does not make the produced NovaOryn EFI image a Windows application, and the NovaOryn bootstrap project removes the stock Windows CoreLib and platform runtime libraries before compilation and linking.

A future NovaOryn SDK runtime pack may expose a dedicated `novaoryn-x64` RID after its own runtime graph and distributable ILC pack exist. Version 0.0.21 does not invent a misleading RID merely to alter build output.

## Expected result

The managed solution build no longer reports `win-x64` for `NovaOryn.Kernel.Sample`. The dedicated bootstrap publish may still show `win-x64` while selecting the Microsoft x64 ILC package, but generated global usings are no longer compiled into the custom system module.
