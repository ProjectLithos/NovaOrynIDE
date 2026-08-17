# NovaOryn 0.3.2

NovaOryn 0.3.2 is a corrective usability release for roadmap item 10, kernel address-space design.

## Changes

- Adds `KernelAddressSpace.GetLastStatusName()` so freestanding kernels can print a stable symbolic status without enum formatting, `ToString()`, interpolation, or `string.Format`.
- Adds `KernelConsole.WriteHex(UInt64)` for allocation-free, fixed-width hexadecimal diagnostics.
- Updates the authoritative bootstrap, command-line template, and Visual Studio template to print the address-space status and the standard kernel image, heap, stack, direct-map, MMIO, and page-table-window bases.
- Prints the status before returning from a failed address-space initialization so initialization failures are visible on serial/framebuffer output.
- Adds source-policy regression checks for the freestanding-safe diagnostics.

There are no kernel address-space layout changes and no PMM/VMM semantic changes in this release.
