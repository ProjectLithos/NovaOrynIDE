# NovaOryn IDE 0.1.53

NovaOryn IDE 0.1.53 fixes the bundled NovaOryn SDK 0.38.0 public-contract build failure caused by undocumented public version constants in `NovaOrynSdkContract`.

The stable API/ABI values are unchanged. XML documentation is now present for every public contract constant and compatibility method parameter/return value, so SDK projects that enforce CS1591 compile successfully.
