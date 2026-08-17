# NovaOryn 0.0.32 — Visual Studio Return-Value Policy Correction

This release corrects the Visual Studio extension methods rejected by the NovaOryn source-policy tests.

## Corrected methods

- `NovaOrynLaunchService.QueueLaunch(bool)` now returns `bool`.
- `NovaOrynOutputPane.WriteLine(string)` now returns `bool`.
- `NovaOrynOutputPane.Activate()` now returns `bool`.

The output-pane methods translate the Visual Studio HRESULT result into a Boolean success value. `QueueLaunch` returns `true` after the asynchronous launch operation has been queued.

## Behaviour retained

- F5 and Ctrl+F5 interception.
- NovaOryn kernel-project recognition.
- Build and Run commands in the Tools menu.
- Build output streaming into the NovaOryn OS SDK pane.
- Existing NativeAOT, EFI image, QEMU, serial and CPU halt pipeline.

Run `Update-NovaOryn.bat`, then `Build-NovaOryn.bat`.
