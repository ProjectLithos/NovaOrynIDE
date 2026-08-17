# NovaOryn 0.0.88

NovaOryn 0.0.88 fixes a C# compilation error in `NovaOryn.SourcePolicy.Tests`.

The 0.0.87 test additions declared the top-level local variable `visualStudioConsole` twice in `Program.cs`. C# top-level statements share one local scope, so the second declaration produced `CS0128` before the policy tests could run.

The second variable is now named `visualStudioTemplateConsole`, and the two associated checks use that distinct name. A structural scan confirms that the test runner has no duplicate top-level local declarations after the correction.

This release does not alter the kernel, the separated low-level DLL, `KernelConsole.Write` or `KernelConsole.WriteLine`, the freestanding `System.String` implementation, GDT/TSS, IDT, interrupt controllers, NativeAOT compilation, linking, image creation, or QEMU launch behaviour.

The 0.0.87 release note is intentionally retained. NovaOryn release notes are no longer deleted merely because a newer development build is produced.
