# NovaOryn IDE 0.10.7

## TypeScript build correction

The bottom-panel MutationObserver repair callback in 0.10.6 used an expression-bodied arrow declared as returning `void`.

`window.requestAnimationFrame(...)` returns a numeric request ID, so TypeScript reported:

`TS2322: Type 'number' is not assignable to type 'void'.`

0.10.7 changes the callback to a block-bodied function and deliberately discards the request ID.

All 0.10.6 bottom-panel and device/driver changes are retained.
