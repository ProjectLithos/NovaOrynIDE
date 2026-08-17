# NovaOryn 0.35.17

NovaOryn 0.35.17 fixes missing nested SDK and Userland project descriptors in projects created by the Visual Studio template.

The main kernel project was instantiated, but Visual Studio then reported missing references such as:

`Sdk\NovaOryn.Kernel.Console\NovaOryn.Kernel.Console.csproj`

The template source contains those projects, so the VSIX packaging stage now isolates the template engine from nested project-file semantics.

Each project template is packaged through a disposable staging tree:

- the primary `Project/@File` remains the one real `.csproj`;
- every other `.csproj` in the template is renamed in staging to `<name>.csproj.template`;
- the matching `ProjectItem` source is rewritten to the neutral filename;
- `ProjectItem/@TargetFileName` remains the intended `.csproj` path.

Visual Studio therefore copies the nested project descriptor as an ordinary template file and writes it to the exact path that kernel `ProjectReference` entries expect.

The repository source tree is not renamed. SDK and Userland projects remain ordinary `.csproj` files for development, policy tests, source browsing and build tooling.

The VSIX build now rejects any project-template ZIP that:

- contains more than one raw `.csproj`;
- omits a neutral nested-project payload;
- lacks an exact neutral-source to `.csproj` target mapping.

The template policy test also verifies that explicit SDK `ProjectReference` paths resolve in the template source before packaging.
