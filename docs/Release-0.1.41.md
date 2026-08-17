# NovaOryn IDE 0.1.41

NovaOryn IDE 0.1.41 adds conditional/hit-count source breakpoints and a persistent Watch window with live expression evaluation to the working NativeAOT/QEMU debugger.

## Conditional and hit-count breakpoints

Right-click a C# source line and use **Debug -> Edit Breakpoint Condition…** or **Debug -> Edit Breakpoint Hit Count…**. If the line has no breakpoint yet, NovaOryn creates one automatically. Conditions and hit-count rules persist across IDE restarts and are sent to the debugger before KMain is released.

Conditions use NovaOryn's native paused-expression evaluator. It currently supports x64 integer registers, decimal/hex integer literals, parentheses, arithmetic, bitwise, logical and comparison operators, plus `[address]` for a 64-bit guest-memory read. Examples include `rax == 0x10`, `(rflags & 1) != 0`, and `[rsp+8] == 0`.

Hit-count rules support `N`/`=N` (exact hit), `>=N`, `>N`, `<=N`, `<N`, and `%N` (every Nth hit). The debugger maintains the live hit counter and automatically resumes QEMU when a breakpoint's hit/condition rules do not match.

## Watch and expression evaluation

The **NovaOryn Debug** inspector now contains a persistent Watch section. Watch expressions are saved in IDE local storage and reevaluated each time the kernel pauses. Values are displayed in both 64-bit hexadecimal and decimal form. Watch reads are serialized through the QEMU GDB RSP connection so they cannot collide with one another.

The current SDK debug map still does not export named C# local-variable metadata. Consequently, 0.1.41 watch expressions are native-aware rather than pretending unresolved C# locals exist: use registers and address expressions such as `rsp + 8` or `[rbp-0x10]`. Named C# locals can be layered onto the same evaluator when the SDK begins exporting their NativeAOT locations.
