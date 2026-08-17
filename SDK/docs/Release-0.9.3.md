# NovaOryn 0.9.3

NovaOryn 0.9.3 corrects the freestanding native link after the 0.9.2 human-readable numeric formatting caused NativeAOT/ILC to emit Win64 stack-protector instrumentation.

- The freestanding x64 runtime now exports `__security_cookie`.
- It also exports `__security_cookie_complement` for the standard compiler ABI.
- `NovaOrynRuntimeInitialize` seeds the cookie from the timestamp counter, live bootstrap stack address, and image-relative address before managed code executes.
- No Windows CRT or Windows NativeAOT runtime library is linked.
- The 0.9.2 human-readable `KernelConsole` formatting is retained unchanged.
- Build policy requires the security-cookie ABI to remain present.
