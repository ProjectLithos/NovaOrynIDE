# NovaOryn 0.0.94

NovaOryn 0.0.94 replaces the framebuffer console's partial 5×7 character table with the first complete NovaOryn bitmap typeface.

## NovaOryn Mono

The kernel now embeds **NovaOryn Mono**, a fixed-width monochrome font containing every printable ASCII character from U+0020 through U+007E.

The release includes:

- all 26 uppercase letters;
- all 26 lowercase letters;
- all ten decimal digits;
- complete printable ASCII punctuation and symbols;
- distinct lowercase glyphs rather than uppercase substitutions;
- below-baseline descenders for `g`, `j`, `p`, `q`, and `y`;
- a visible replacement question mark for unsupported bytes or characters;
- one authoritative rendered `FontSize`, expressed as glyph height in framebuffer pixels.

The previous implementation only contained the characters needed by the two boot acceptance messages and returned one generic placeholder for almost everything else. That table has been removed.

## Renderer integration

The embedded raster master is 8×16 pixels, but it is not exposed as the console's rendered size. The renderer now consumes `FontSize` directly and derives rendered glyph width, character advance, line height, wrapping, clipping, and raster sampling from it.

The default `FontSize` is 32 pixels, producing 16×32 rendered glyphs, a 20-pixel character advance, and a 40-pixel line height. The reusable framebuffer configuration no longer exposes a separate `Scale` property. Supported font sizes range from 8 through 128 pixels.

Generated kernels now call `KernelConsole.Initialize(boot, 32U)`, making the exact renderer size visible at the use site. Kernel authors can select any supported pixel height with that overload, and `KernelConsole.FontSize` reports the accepted size. The same freestanding font and renderer are copied byte-for-byte into:

- the authoritative in-repository kernel console assembly;
- the command-line-created kernel project template;
- the Visual Studio kernel project template.

The reusable `NovaOryn.Console.Framebuffer` assembly contains the same glyph data, accepts the rendered pixel height through `FramebufferConfiguration.FontSize`, provides `FramebufferConfiguration.Default(fontSize)`, and reports the active value through `FramebufferConsole.FontSize`.

## Validation

Source-policy validation now checks that:

- every printable ASCII code has both 8-row source halves;
- reusable and freestanding glyph data are identical;
- all three freestanding copies are identical;
- all three freestanding framebuffer renderers are identical;
- `FontSize` is the single public rendered-size input;
- no public `Scale` property or `configuration.Scale` use remains;
- the five lowercase descenders contain pixels in the final three source rows.

The release stores both the embedded 8×16 raster-master specimen and a 32-pixel specimen rendered with the same dimensions and sampling used by the console.

## Typeface provenance

The embedded monochrome glyph shapes were rasterised from DejaVu Sans Mono Bold 2.37 and renamed **NovaOryn Mono**. NovaOryn does not distribute the source font file. The applicable Bitstream Vera/DejaVu notice is retained in `THIRD-PARTY-NOTICES.md`.
