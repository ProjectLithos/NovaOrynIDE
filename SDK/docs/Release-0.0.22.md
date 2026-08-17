# Nova Oryn OS SDK 0.0.22

## Purpose

This release corrects the custom CoreLib compilation failure reported for `System.Object._methodTable`.

## Cause

NovaOryn treats all warnings as errors. The method-table field in the custom `System.Object` layout is required by NativeAOT-generated code, but it is intentionally never read or written by ordinary C# source. Roslyn therefore emitted `CS0169`, which became a build error.

## Correction

- retains the private `IntPtr _methodTable` object-header field
- suppresses only `CS0169` around that exact field
- restores the warning immediately afterwards
- keeps repository-wide `TreatWarningsAsErrors` enabled
- adds a source-policy regression test requiring the field and the narrowly scoped suppression
- updates all product and build metadata to `0.0.22`

## Expected result

The no-standard-library bootstrap project should pass the Roslyn compile stage that previously failed with:

```text
error CS0169: The field 'object._methodTable' is never used
```

The build will then continue into ILC, where any additional missing custom-CoreLib contracts will be reported explicitly rather than being hidden.
