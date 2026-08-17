# NovaOryn 0.25.6

NovaOryn 0.25.6 corrects the build-policy regression introduced in 0.25.5.

`NovaOryn.BuildPolicy.Tests` referenced a `bootstrap` variable before any such variable was declared. The policy now reads the authoritative bootstrap into `bootstrapKernel` before checking that the compile-time PS/2 contract constant is not redundantly compared at runtime.

No runtime PS/2, keyboard-layout, `NovaOryn.String`, CoreLib, or kernel functionality is changed by this corrective release.
