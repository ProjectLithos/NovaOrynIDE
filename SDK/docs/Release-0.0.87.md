# NovaOryn 0.0.87

NovaOryn 0.0.87 fixes the blank framebuffer that appeared after the console and low-level services were separated into dedicated freestanding assemblies.

The framebuffer had already initialized and cleared successfully. Execution then entered the placeholder `System.String` character indexer while processing the first `KernelConsole.WriteLine` call. That placeholder deliberately contained a non-terminating loop, so neither serial nor framebuffer output could reach the first character.

The freestanding CoreLib now models the NativeAOT string layout with `_stringLength` followed by `_firstChar`, provides a terminating character indexer over the inline UTF-16 data, and retains `RuntimeHelpers.OffsetToStringData` for pinned-string lowering. `KernelConsole.Write(String)` now pins the managed string and reads its character buffer directly before mirroring each character to the hidden serial and framebuffer implementations.

`Kernel.cs` remains high-level only. It still contains normal `KernelConsole.WriteLine(...)` and `KernelPlatform` calls and exposes no `DllImport`, native port I/O, framebuffer implementation, or runtime-entry code.

The authoritative bootstrap, command-line template, and Visual Studio template contain identical CoreLib and console implementations. Source-policy tests reject the former infinite string indexer and any return to `value[index]` in the kernel console path.
