# NovaOryn 0.25.2

Corrective release for the decoded PS/2 input acceptance policy.

- Refreshes the authoritative bootstrap and both installable kernel templates with the decoded-key consumer used since 0.24.2.
- Build policy now validates decoded Up/Down and D1/D2/D3 behaviour in both the SDK bootstrap and generated kernel template without depending on one exact source-code spelling.
- KernelConsole remains prohibited from reading i8042 ports directly; KernelPs2 remains the sole PS/2 hardware owner.
- No change to the 0.25.0 NovaOryn.String/CoreLib architecture.
