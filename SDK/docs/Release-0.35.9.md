# NovaOryn 0.35.9

NovaOryn 0.35.9 fixes Visual Studio user-template discovery after 0.35.8.

The installer now copies `NovaOrynKernel.zip` directly into the Visual Studio user `Templates\ProjectTemplates` root rather than a nested `NovaOryn` directory, verifies that `NovaOrynKernel.vstemplate` is at the ZIP root, removes the obsolete nested copy, and runs `devenv.exe /installvstemplates` while Visual Studio is closed to rebuild the template cache immediately.
