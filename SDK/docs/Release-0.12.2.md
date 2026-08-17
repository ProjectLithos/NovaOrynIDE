# NovaOryn 0.12.2

NovaOryn 0.12.2 corrects the freestanding public contract surface introduced by roadmap item 19.

## Fixed

- Driver-facing value types no longer use C# `record struct` declarations.
- The contracts are ordinary immutable `readonly struct` types with explicit constructors and normal .NET-style getter properties.
- This removes accidental dependencies on `System.IEquatable<T>`, `System.Runtime.CompilerServices.IsExternalInit`, `System.Text.StringBuilder`, and compiler-generated record overrides that are not present in `NovaOryn.Freestanding.CoreLib`.
- `NovaOryn.Drivers.Tests` no longer uses the record-only `with` expression when constructing an invalid interrupt-priority test case.
- The authoritative SDK source, command-line kernel template, and Visual Studio kernel template contain the same corrected contracts.

## Retained from 0.12.1

- The normal driver/device registries remain dynamically growable from the already-initialized `KernelHeap`.
- 64 driver entries and 128 device entries remain initial capacities only.
- Explicit fixed-capacity mode remains available for deterministic RTOS and safety-oriented kernels.
- Driver interrupt requests remain transport-neutral and do not expose PIC, I/O APIC, MSI, or MSI-X details to drivers.

## Roadmap

Item 20 remains storage and filesystems.
