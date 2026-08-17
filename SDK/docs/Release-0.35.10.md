# NovaOryn 0.35.10

NovaOryn 0.35.10 fixes project-template installation on machines with multiple Visual Studio generations installed.

The installer now uses `vswhere` to enumerate Visual Studio instances and selects the newest supported installation rather than preferring hard-coded Visual Studio 2022 paths. For Visual Studio 18/2026 it installs `NovaOrynKernel.zip` under `Documents\Visual Studio 18\Templates\ProjectTemplates`, while Visual Studio 2022 continues to use its own user-template directory.

The installer also removes only positively identified legacy template archives whose `.vstemplate` metadata names the old `Oryn OS Project`, then runs the selected instance's `devenv.exe /installvstemplates` to rebuild the project-template cache.

This specifically addresses the symptom where the New Project dialog still showed an old `Oryn OS Project 0.13.12` recent template while searching for `Nov` did not find the current NovaOryn template.
