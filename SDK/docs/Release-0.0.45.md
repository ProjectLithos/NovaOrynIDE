# Nova Oryn OS SDK 0.0.45

## Documentation filename correction

Version 0.0.45 corrects Windows path failures while generating the SDK usage site.

The documentation generator previously used the complete public-item identifier as the HTML filename. Constructors and methods with long parameter lists could therefore exceed the Windows path-component limit.

API pages now use a short readable name followed by a deterministic SHA-256-derived suffix. The full assembly, qualified name and signature remain in the page content and search index. The suffix prevents overloaded members from colliding without exposing long signatures in filesystem paths.

Run:

```bat
Build-NovaOrynDocumentation.bat
```

The generated site remains at `docs\site\index.html`.
