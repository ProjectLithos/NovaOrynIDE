# NovaOryn 0.4.9

NovaOryn 0.4.9 corrects the independent policy-test build integration introduced in 0.4.9.

The seven policy executable projects now have explicit Debug and Release solution build mappings. In addition, `Build-NovaOryn.ps1` builds every policy project individually immediately before executing it, so policy tests remain independent and cannot be skipped because of solution configuration metadata.

No PMM, VMM, address-space, heap, framebuffer, documentation, or kernel runtime algorithm is changed by this corrective release.
