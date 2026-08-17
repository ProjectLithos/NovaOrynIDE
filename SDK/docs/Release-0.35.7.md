# NovaOryn 0.35.7

0.35.7 fixes project-template discovery after installing the NovaOryn Visual Studio extension.

0.35.6 proved that `ProjectTemplates/NovaOrynKernel.zip` was physically present in the completed VSIX, but Visual Studio still did not list the NovaOryn template because a post-pack physical insertion is not sufficient by itself: the completed `extension.vsixmanifest` must register the payload as a `Microsoft.VisualStudio.ProjectTemplate` asset.

The deterministic VSIX post-pack helper now opens the completed package, inserts the template ZIP, repairs or creates the `Microsoft.VisualStudio.ProjectTemplate` asset in `extension.vsixmanifest`, normalizes its path to `ProjectTemplates/NovaOrynKernel.zip`, updates the OPC content type for the nested ZIP, and reopens the VSIX to verify both the physical payload and the manifest registration. `Build-NovaOrynVSIX.ps1` independently repeats the final manifest check before publishing the artifact.
