# NovaOryn 0.0.91

NovaOryn 0.0.91 fixes the freestanding CoreLib build failure reported as `CS0649` for `System.String._stringLength`.

The freestanding string object deliberately mirrors the NativeAOT runtime layout with `_stringLength` followed by inline character data beginning at `_firstChar`. Those fields are not assigned by ordinary C# constructors: NativeAOT materializes string literals and populates the object layout directly. With repository-wide warnings treated as errors, Roslyn therefore rejected `_stringLength` as an apparently unassigned field before ILC could run.

The authoritative CoreLib, command-line kernel template, and Visual Studio kernel template now wrap only the two runtime-owned string-layout fields in a narrowly scoped `#pragma warning disable CS0649` / `#pragma warning restore CS0649` pair. The fields, order, types, indexer, and managed `KernelConsole.Write`/`WriteLine` APIs remain unchanged.

A source-policy regression check requires the suppression to remain narrowly scoped. No GDT/TSS, IDT, interrupt-controller, console rendering, native I/O, linker, image, or QEMU behaviour changes in this release.
