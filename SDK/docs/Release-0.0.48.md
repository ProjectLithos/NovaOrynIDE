# NovaOryn 0.0.48

## Documentation audit build correction

This maintenance release corrects the documentation generator introduced in 0.0.47.

- Fixed the invalid C# escape sequences in the public API audit status message.
- Preserved the complete Windows output path in the console message.
- The documentation generator can now compile and produce the SDK usage site and public API audit.
- Updated SDK, toolchain, template and VSIX version metadata to 0.0.48.

Run `Build-NovaOrynDocumentation.bat` after applying the release. The expected outputs are:

- `Artifacts\Documentation\site\index.html`
- `Artifacts\Documentation\PublicApiAudit.json`
