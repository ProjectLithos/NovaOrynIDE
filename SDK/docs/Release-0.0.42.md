# Nova Oryn OS SDK 0.0.42

## Generated SDK usage site

Release 0.0.42 adds `NovaOryn.DocumentationGenerator`, a repository-owned executable that discovers configured public and SDK-tool assemblies, scans their C# source declarations and XML documentation, records project-reference dependencies, and generates a static offline HTML site in `docs/site`.

The generated reference provides assembly pages, individual public-item pages, source locations and a browser-side search index. API pages reserve dedicated sections for what an item does, when to use it, dependencies, return values and examples.

The documentation format uses standard XML comments plus `<nova.when>` and `<nova.depends>` metadata. Strict completeness checks are intentionally disabled for this foundation release so the existing API remains buildable. Release 0.0.43 can complete the public API audit and enable mandatory summaries, usage guidance and examples.

`Build-NovaOryn.ps1` builds and runs the documentation generator before source-policy tests. The source-policy suite verifies the generator project, configuration, guides, generated site and build integration.
