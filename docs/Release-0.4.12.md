# NovaOryn IDE 0.4.12

NovaOryn IDE 0.4.12 bundles NovaOryn SDK 0.41.6 and fixes interactive-shell compatibility for existing generated OS projects. The SDK-owned command-line input bridge now checks the established PS/2 capabilities contract instead of requiring a newly-added `KernelPs2.IsInitialized()` member, preventing mixed project-local SDK copies from failing compilation during refresh.
