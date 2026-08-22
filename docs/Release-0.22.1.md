# NovaOryn IDE 0.22.1

0.22.1 fixes the QEMU x64 Debug rendezvous and termination diagnostics discovered after 0.22.0.

- The private pre-KMain debug rendezvous now parks the guest CPU with `hlt` instead of continuously executing a `pause`/jump loop. The incoming RFLAGS value is preserved and restored on resume so firmware execution semantics are unchanged.
- The IDE now decodes abnormal Windows QEMU termination statuses, including `0xCFFFFFFF`, and reports them as QEMU/debug-session failures rather than opaque decimal build exit codes.
- The final QEMU serial tail is appended before a debug session is completed, preserving the evidence needed to distinguish a pre-KMain boot stall from a user/host termination.
- Normal QEMU closure remains a successful end of the debug session.
- The 0.22.0 formal network-stack API is unchanged.
