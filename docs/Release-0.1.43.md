# NovaOryn IDE 0.1.43

NovaOryn IDE 0.1.43 adds the next two kernel-debugger capabilities: a live guest-memory viewer and named NativeAOT C# locals/arguments.

## Memory viewer

The **NovaOryn Debug** inspector now contains a Memory section while the kernel is paused. The address field accepts the same integer/register expression syntax as Watch, including values such as `rsp`, `rbp-0x40`, or an absolute address. Reads are performed through QEMU's GDB remote stub and are rendered as 16-byte rows with runtime address, hexadecimal bytes, and ASCII. Read sizes of 64, 128, 256, 512, and 1024 bytes are available, and the selected address/size persist between IDE runs.

Memory and Watch refreshes are serialized so QEMU's one-outstanding-request GDB RSP channel is not used concurrently.

## Named C# locals and arguments

When a Debug build's `MinimalKernel.pdb` contains NativeAOT CodeView local-variable records, the debugger now uses the bundled `llvm-pdbutil` to load `S_LOCAL` and supported `S_DEFRANGE_*` live ranges. It resolves active values from x64 registers or register-relative memory at the stopped RIP and presents the C# name, argument/local kind, value, and native location in the Locals panel.

The implementation recognizes register, register-relative, and frame-pointer-relative CodeView live ranges. If the PDB does not contain a usable live range for the current instruction, NovaOryn falls back to the existing native frame/stack slot display rather than inventing a value.

Named variables are also available to the existing Watch/conditional expression evaluator whenever their active NativeAOT location can be resolved.
