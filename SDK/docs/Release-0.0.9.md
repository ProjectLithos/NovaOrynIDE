# Nova Oryn OS SDK 0.0.9

## Purpose

Version 0.0.9 fixes the repository-local .NET SDK bootstrap after the source commit has been pushed.

## Corrected behaviour

The official `dotnet-install.ps1` script writes progress information to the PowerShell success stream. The previous installer returned that output together with the executable path, causing the combined text to be invoked as a command.

The installer now:

- streams .NET installer progress directly to the console
- records the native installer exit code separately
- computes the expected `dotnet.exe` path after installation
- passes only that exact path to the NativeAOT restore step
- reuses the already installed .NET SDK 10.0.302
- resumes with NativeAOT/ILC package restoration without downloading .NET again

## Required order

Source extraction, commit, and push still occur before any toolchain installation.
