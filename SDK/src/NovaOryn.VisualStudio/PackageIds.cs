using System;
namespace NovaOryn.VisualStudio;
internal static class PackageIds
{
    public const string PackageGuidString = "7b274f30-bec4-4968-a8e2-b031cb57678a";
    public static readonly Guid CommandSet = new("4fc64cd0-9d0f-4246-ae6d-d3b67fbc2992");
    public const int BuildCommand = 0x0100;
    public const int ConfigureCommand = 0x0102;
    public const int RunCommand = 0x0101;
}
