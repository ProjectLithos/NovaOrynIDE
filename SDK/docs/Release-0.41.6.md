# NovaOryn SDK 0.41.6

NovaOryn SDK 0.41.6 fixes interactive-shell compatibility with existing generated projects. The SDK command-line input bridge no longer requires the newly-added `KernelPs2.IsInitialized()` member; it uses the established `KernelPs2.GetCapabilities()` contract so refreshed command-line code can compile against older project-local PS/2 SDK copies while still detecting controller/keyboard readiness.
