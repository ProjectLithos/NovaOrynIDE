# Nova Oryn OS SDK 0.0.40

## Structured-template source-policy correction

The source-policy test now validates the kernel entry source at its structured location:

```text
templates/NovaOrynKernel/Kernel/Kernel.cs
```

Version 0.0.39 correctly moved the generated kernel files into purpose-specific folders, but the test still checked the previous flat path. This release updates the test to match the intended project layout. No kernel, template, build, run, QEMU, framebuffer, serial, or halt behaviour changes.
