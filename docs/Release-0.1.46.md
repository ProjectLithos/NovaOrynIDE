# NovaOryn IDE 0.1.46

NovaOryn IDE 0.1.46 fixes release-version drift in the dependency compatibility gate and active IDE metadata.

## Fix

The 0.1.45 package manifests were correctly versioned as 0.1.45, but `Validate-NovaOrynIDEDependencies.ps1` and several active host/UI/version strings still expected or displayed 0.1.43. This caused `Build-NovaOrynIDE.bat` to fail before dependency validation with `Root package version is 0.1.45; expected 0.1.43.`

0.1.46 makes the active build, validator, package manifests, workspace verifiers, security baseline, launcher and UI version strings agree on 0.1.46. Historical release notes are intentionally unchanged.
