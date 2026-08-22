# NovaOryn IDE 0.16.1

NovaOryn IDE 0.16.1 is a regression-packaging correction for the 0.16.0 synchronization release.

The synchronization primitives introduced in 0.16.0 are unchanged. The release verifier now validates the complete toolbar source and checked-in JavaScript runtime for the 0.15.1 Kernel Console rule instead of extracting a method with a fragile regular expression.

The ChangedFiles package also deliberately includes the known-good `novaoryn-toolbar-widget.tsx` and `novaoryn-toolbar-widget.js` files. This repairs installations that reached 0.16.0 through an incomplete earlier ChangedFiles chain and still contained the pre-0.15.1 Run/Debug auto-attach behaviour.
