# NovaOryn IDE 0.1.40

NovaOryn IDE 0.1.40 extends the working NativeAOT/QEMU source-breakpoint debugger with source-oriented stepping and paused-state inspection.

- Step Into advances until execution reaches a different mapped C# source line.
- Step Over recognizes x64 CALL instructions and uses a temporary post-call breakpoint rather than entering the called routine.
- Step Out continues to the current native frame return address.
- Continue, Pause, Restart and Stop keep their existing QEMU GDB behaviour.
- Paused execution now has a current-statement arrow and whole-line editor highlight.
- A NovaOryn Debug inspector shows the call stack, x64 integer registers and native frame/stack slots.
- Call-stack source frames can be clicked to open their C# location.
- Frame-pointer stack walking falls back to conservative stack return-address discovery when NativeAOT does not preserve a conventional RBP chain.
- Existing 0.1.39 source-map compatibility and verified breakpoint binding remain in place.

The current SDK debug JSON does not export named C# local-variable records, so 0.1.40 labels the Locals section as native frame data rather than presenting guessed C# local names.
