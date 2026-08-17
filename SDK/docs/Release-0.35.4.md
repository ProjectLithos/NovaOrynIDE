# NovaOryn 0.35.4

Visual Studio now deploys the NovaOryn multi-project template as one compressed template archive, as required for reliable ProjectGroup child-project instantiation. Userland command, settings, fonts, images, and drivers source files are included in the child projects instead of leaving empty project shells. The VSIX build validates the nested template archive and every required Userland source item before producing the release artifact.
