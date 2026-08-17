# NovaOryn 0.34.1

NovaOryn 0.34.1 fixes the command-line build failure found by the fast selected-kernel build.

- Replaces the static fixed-buffer command input store with a zero-filled 256-byte `KernelHeap` allocation.
- Removes the CS0169 unused-field failure without suppressing compiler diagnostics.
- Adds the explicit `NovaOryn.Kernel.Heap` dependency to the command-line project.
- Mirrors the source and project-reference correction into both generated-kernel SDK templates.
- Keeps the public command-line API and keyboard behaviour unchanged.
