# NovaOryn IDE 0.19.1

This maintenance release repairs authoritative-configuration version consistency after the 0.19.0 executable/application-format release.

The generator and configurator version surfaces are explicitly synchronized to 0.19.1, and the ChangedFiles package deliberately contains both TypeScript source files and their checked-in JavaScript runtime counterparts. This prevents an older generator/configurator from surviving when a release is applied over an earlier source tree.

The 0.19.0 executable/application-format implementation remains unchanged.
