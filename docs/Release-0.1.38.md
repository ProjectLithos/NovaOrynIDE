# NovaOryn IDE 0.1.38

NovaOryn IDE 0.1.38 fixes source breakpoints that could appear in the editor while the NativeAOT kernel ran straight through them.

- Requested breakpoints are resolved and armed while QEMU is still held before `KMain`.
- Non-executable C# lines such as braces, declarations and blank/comment lines bind to the nearest executable NativeAOT sequence point in the same source file, preferring the next line.
- Debug output records both the requested line and the executable line together with the relocated runtime address.
- If any requested breakpoint still cannot be verified, NovaOryn does not silently release the kernel. QEMU remains paused before `KMain` until the breakpoint is moved/removed and the user presses Continue.
- Breakpoint hits report and reveal the actual executable C# line.
- Removing an unverified breakpoint during the pre-`KMain` hold now removes the pending request instead of accidentally retrying it.

The bundled NovaOryn SDK remains 0.37.4.
