# NovaOryn 0.9.2

NovaOryn 0.9.2 improves kernel-startup diagnostics so numbers are presented in human-readable form wherever possible.

- Counts and logical indices are printed in ordinary decimal notation.
- Clock frequencies are scaled automatically to Hz, kHz, MHz, or GHz.
- Nanosecond durations are scaled automatically to ns, us, ms, or s.
- Physical-memory accounting is shown in B, KiB, MiB, GiB, or TiB.
- Addresses, selectors, masks, and other bit-level values remain hexadecimal.
- The freestanding `KernelConsole` exposes `WriteUInt64`, `WriteByteSize`, `WriteFrequency`, and `WriteDurationNanoseconds` without heap allocation.
- The command-line and Visual Studio templates use the same formatting and are policy-tested.
