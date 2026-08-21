# NovaOryn IDE 0.14.9

Corrects the dependency fingerprint implementation used by the 0.14.8 build-state manager. Windows PowerShell 5.1 runs on .NET Framework, where `System.Convert.ToHexString` is unavailable.

The SHA-256 fingerprint now converts bytes using `BitConverter.ToString(...).Replace("-", "").ToLowerInvariant()`, preserving the exact lowercase hexadecimal fingerprint semantics while remaining compatible with the Windows PowerShell runtime used by `Build-NovaOrynIDE.bat`.

No dependency-staleness policy has changed: npm dependencies are invalidated only when dependency declarations/pins change, while the completed IDE build marker remains version-specific.
