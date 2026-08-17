# NovaOryn 0.0.96

NovaOryn 0.0.96 fixes the complete-solution compile failure in `NovaOryn.Console.Framebuffer`.

## Cause

The real 8×16 font introduced a row-based `BitmapFont.GetGlyphRow` API. A release could update `BitmapFont.cs` while an installed tree retained the previous `FramebufferConsole.cs`, leaving the reusable console calling the removed packed 5×7 `BitmapFont.GetGlyph` method. The compiler then reported CS0117 before the kernel build could begin.

## Correction

- The complete coupled font pipeline is reissued: the reusable framebuffer assembly, freestanding kernel renderer, command-line template, and Visual Studio template. This repairs installations that applied 0.0.95 without first receiving every 0.0.94 font file.
- `src/NovaOryn.Console.Framebuffer/BitmapFont.cs` exposes `TryGetGlyphRow`, and its renderer consumes that API instead of the removed packed-glyph method.
- Every freestanding renderer validates bitmap-font contract version 2 before writing pixels.
- The three generated freestanding font, renderer, and kernel-console copies are byte-identical to their authoritative SDK sources.
- Source-policy validation rejects any remaining `BitmapFont.GetGlyph(` caller and verifies the current row-based contract.
- `VersionInfo.Current`, the image-builder artifact version, documentation, toolchain metadata, VSIX metadata, and generated-project requirements are aligned to 0.0.96.

The embedded NovaOryn Mono glyph data is unchanged. `Kernel\Kernel.cs` remains the user-owned source file, and one `ConsoleFontSize` value controls the renderer's actual glyph height.
