# Nova Oryn OS SDK 0.0.46

## Disposable documentation output

Version 0.0.46 moves generated HTML documentation from the tracked `docs\site` source directory to:

```text
Artifacts\Documentation\site
```

`Artifacts` is already ignored by Git. Documentation generation therefore no longer changes tracked source files, even when generation stops partway through.

`Build-NovaOrynDocumentation.ps1` now reads `outputDirectory` from `docs\NovaOryn.Documentation.json` and validates `index.html` and `search-index.json` beneath that configured directory.

The tracked placeholder site files introduced in 0.0.42 are removed. Author-maintained documentation remains in `docs\site-content`.

Run:

```bat
Build-NovaOrynDocumentation.bat
```

Then open:

```text
C:\NovaOryn\Artifacts\Documentation\site\index.html
```
