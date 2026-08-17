# Nova Oryn OS SDK 0.0.24

## Purpose

This release fixes the direct .NET 10 ILC failure `Expected type System.Buffer not found in module NovaOryn.Kernel.Bootstrap`.

## Root cause

The bootstrap invocation used `-O`. In .NET 10 ILC, optimisation implies the IL scanner. The scanner constructs a broad JIT-helper cache and eagerly resolves helper owners such as `System.Buffer`, even when the minimal no-GC `KMain` path does not perform a managed-reference bulk copy.

## Correction

The bootstrap ILC invocation now uses:

```text
--noscan
--reflectiondata:none
--nopreinitstatics
```

The `-O` option is removed for this first correctness milestone. Optimisation will be re-enabled only after `NovaOryn.RuntimePack.X64` intentionally implements the corresponding CoreLib and runtime helper contracts.

The custom system module also defines `System.Buffer.BulkMoveWithWriteBarrier`. In the no-GC bootstrap this method is a fail-fast boundary: reaching it indicates that managed-reference copying was introduced before a write barrier and GC exist. It does not silently copy references without a GC barrier.

## Acceptance

The source-policy tests require scanner-disabled direct ILC compilation, reject `-O` in the bootstrap compiler, and require the .NET 10 `System.Buffer` contract.
