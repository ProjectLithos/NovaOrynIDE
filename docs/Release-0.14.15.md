# NovaOryn IDE 0.14.15

This release corrects the Engineering-window release verification used by the final build gate.

Engineering tools continue to open in the main document area when first attached. After a user moves a tool to another dock area, NovaOryn leaves that user-selected layout alone. Run/Debug follows the same first-attach rule for Kernel Console.

The 0.14.15 release verifier no longer relies on one large regular expression to infer this behaviour. It extracts and inspects the actual `showEngineeringWidget` method and checks the explicit Run/Debug attachment statement. The known-good Engineering source files are also included in ChangedFiles so applying a patch release cannot leave an older docking implementation behind.
