# NovaOryn 0.0.58

## GDT and TSS

This release adds the professional x64 descriptor-table foundation.

- Adds `SegmentSelector`, `DescriptorPrivilegeLevel`, `GlobalDescriptorTableConfiguration`, `TaskStateSegmentConfiguration`, `IGlobalDescriptorTable`, and `ITaskStateSegment`.
- Adds `NovaOryn.Architecture.X64.Descriptors`.
- Builds null, kernel code/data, user code/data, and 64-bit TSS descriptors.
- Adds native `LGDT`, segment-register reload, far control transfer, and `LTR` wrappers.
- Supports processor-local GDT/TSS instances using caller-owned storage.
- Configures RSP0, IST1 for double fault, IST2 for NMI, and optional IST3 for machine check.
- Supports disabled or deny-by-default I/O permission bitmap policy.
- Adds source-policy checks that ensure the managed and native facilities remain assembled and linked.

## Installation policy

Extract `NovaOryn-ChangedFiles-0.0.58.zip` into `C:\NovaOryn`, commit and push the source changes, and only then run the normal build/toolchain workflow.
