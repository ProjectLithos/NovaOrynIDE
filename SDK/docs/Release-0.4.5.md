# NovaOryn 0.4.5

## Purpose

Correct the 0.4.4 managed solution compile regression in the split freestanding VMM direct-map implementation.

## Changes

- `KernelVirtualMemory.DirectMap.cs` now imports `NovaOryn.Kernel.Internal.X64`, resolving the existing `Native.WritePageTableRoot` call.
- The same correction is applied to the command-line kernel template and Visual Studio kernel template.
- Source-policy tests now require the split direct-map partial class to resolve the low-level `Native` helper namespace before invoking page-table-root operations.
- No virtual-memory algorithm, physical-memory policy, address-space layout, heap behaviour, framebuffer rendering, or documentation-site behaviour changes are introduced.

## Build acceptance

The complete solution must compile past `NovaOryn.Kernel.VirtualMemory`, then run the existing source-policy, memory, VMM, address-space, and heap tests before native kernel build/run acceptance.
