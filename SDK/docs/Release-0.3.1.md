# NovaOryn 0.3.1

NovaOryn 0.3.1 is a corrective release for roadmap item 10, kernel address-space design.

## Corrected build failures

- `KernelAddressSpaceStatus` now declares `Success`, `VirtualMemoryNotInitialized`, `AlreadyInitialized`, and `InvalidLayout` on executable enum-member lines. In 0.3.0 each member was accidentally appended to its `///` XML-documentation line, so the C# compiler treated the member declaration as comment text and the enum was empty.
- `KernelAddressSpace.ValidateRange` now uses the literal `0xFFFFFFFFFFFFFFFFUL` for its overflow boundary. The freestanding CoreLib intentionally omits `UInt64.MaxValue`, so the 0.3.0 spelling could not compile in `NovaOryn.Kernel.AddressSpace`.
- Both generated-kernel template copies receive the same corrections as the authoritative source.
- Source-policy tests require the four status members to remain executable declarations and reject a return to `UInt64.MaxValue` in this freestanding implementation.

## Scope

There are no address-space layout, PMM, VMM, mapping, or API semantic changes. The 0.3.0 kernel address-space design remains unchanged; 0.3.1 makes that implementation compile correctly against NovaOryn's reduced freestanding CoreLib.
