# NovaOryn 0.0.95

NovaOryn 0.0.95 fixes the runtime glyph-selection failure visible as question marks in otherwise valid ASCII boot messages.

## Cause and correction

NovaOryn Mono already contained both 8-row halves for every printable ASCII character. The failure was in the freestanding selection path: two dense 95-case switches selected the packed glyph halves. On the actual NativeAOT kernel path, a repeatable subset of valid byte values reached the replacement-question-mark result instead of its glyph.

The release replaces those dense switches with:

- compile-time `TopXX` and `BottomXX` constants for all U+0020-U+007E glyphs;
- explicit six-range dispatch for each packed half;
- bit-branch selection inside each sixteen-character range;
- no array, static constructor, managed allocation, or switch jump table.

The pixel data, 8×16 raster master, real rendered `FontSize`, spacing, baseline, and descenders are unchanged.

## Editable kernel project

The generated project continues to expose the user-owned source only at `Kernel\Kernel.cs`. The project now includes direct `Build-Kernel.bat` and `Run-Kernel.bat` wrappers in the standalone kernel archive. Both wrappers call the authoritative SDK pipeline with the project manifest, so editing `Kernel.cs` does not require changing the boot entry, framebuffer renderer, NativeAOT entry assembly, linker, image builder, or QEMU command.

## Validation

Source-policy validation verifies that all 95 top and bottom constants exist, reusable and freestanding data are identical, the three freestanding copies are byte-identical, descenders remain present, and no `switch (value)` glyph dispatcher exists.
