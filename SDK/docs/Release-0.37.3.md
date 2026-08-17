# NovaOryn 0.37.3

NovaOryn 0.37.3 makes the optional Linux-kernel console font pack best-effort rather than a prerequisite for the SDK toolchain. Existing downloaded/generated fonts are reused, transient download failures are retried with backoff, and GitHub rate limiting no longer aborts SDK installation.

If optional fonts remain unavailable, NovaOryn continues with its embedded fallback console font and the core .NET/NativeAOT/LLVM/QEMU/NASM toolchain is still considered ready.
