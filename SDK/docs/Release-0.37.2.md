# NovaOryn 0.37.2

NovaOryn 0.37.2 adds an explicit embedded-SDK toolchain mode for NovaOryn IDE. When `NOVAORYN_EMBEDDED_SDK=1`, `Install-NovaOrynToolchain.ps1` installs and validates the repository-local toolchain without requiring the SDK directory itself to be a Git repository or a clean standalone checkout. Standalone SDK installation keeps the existing Git repository and clean-tree safeguards.
