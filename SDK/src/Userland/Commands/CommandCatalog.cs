using System;
namespace NovaOryn.Userland.Commands;
/// <summary>Describes the standard userland commands shipped by the NovaOryn SDK.</summary>
public static class CommandCatalog
{
    /// <summary>Returns the canonical built-in command names.</summary>
    public static String GetNames() => "help\nclear\ncls\necho\ninfo\nsystem\nuptime\nmemory\ndrivers\ndevices\nfont\nbuffering\nkeyboard";
}
