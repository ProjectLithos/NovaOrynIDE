# NovaOryn 0.0.63

This release corrects a false failure in the source-policy test introduced in 0.0.62.

- The freestanding kernel continues to install and visibly report GDT/TSS and IDT initialization.
- The kernel emits those messages one character at a time because its minimal freestanding CoreLib does not provide normal managed string output.
- The source-policy test now validates the real output methods and verifies that each method is called after its corresponding native initialization succeeds.
- The invalid search for whole string literals has been removed.

No kernel initialization facility has been removed or bypassed.
