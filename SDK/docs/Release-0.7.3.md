# NovaOryn 0.7.3

NovaOryn 0.7.3 expands `NovaOryn.Freestanding.CoreLib` for normal allocation-free string operations required by generated kernels and structured diagnostics. It also removes the generated panic logger's dependency on allocating `String.Concat` while the bootstrap remains no-GC.
