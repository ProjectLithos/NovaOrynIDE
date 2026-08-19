# NovaOryn SDK 0.41.7

This SDK accompanies NovaOryn IDE 0.4.13 and preserves uninterrupted hardware IRQ delivery while the IDE debugger is attached. The x64 interrupt ABI continues to export vector-specific stubs used by the debugger without modifying the shared interrupt path.
