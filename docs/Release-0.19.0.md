# NovaOryn IDE 0.19.0

Introduces the formal NovaOryn executable/application format. End-user applications use `.exe`; the container is identified by NovaOryn `NOAP` magic, contains a `.nexe` native image, explicit metadata, dependencies, required capabilities, architecture/ABI, entry-point RVA and immutable resources. Default library conventions are `.dll` and `.lib`. OSes may override visible associations without changing the package ABI. The process loader unwraps and validates the package before using the existing isolated native executable loader.
