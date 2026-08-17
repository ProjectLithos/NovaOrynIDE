using NovaOryn.Boot.Contracts;

namespace NovaOryn.Console.Contracts;

public interface IConsole
{
    bool Initialize(BootContext boot);
    bool Write(ReadOnlySpan<char> text);
    bool WriteLine(ReadOnlySpan<char> text);
    bool WriteLine();
}
