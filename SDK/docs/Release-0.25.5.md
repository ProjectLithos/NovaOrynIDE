# NovaOryn 0.25.5

NovaOryn 0.25.5 corrects the PS/2 input-contract bootstrap guard introduced in 0.25.4.

`KernelPs2.InputContractVersion` is a compile-time constant. Comparing it against `2U` in the bootstrap allowed Roslyn to prove the failure branch unreachable, which is rejected by NovaOryn's warnings-as-errors freestanding build. The runtime comparison has been removed from the authoritative bootstrap and both generated kernel templates.

The decoded keyboard-event contract remains enforced by compiling the call to `KernelPs2.SetKeyboardEventHandler`, while release/build policy still requires PS/2 input contract version 2 and now explicitly rejects reintroducing a runtime comparison against that constant.
