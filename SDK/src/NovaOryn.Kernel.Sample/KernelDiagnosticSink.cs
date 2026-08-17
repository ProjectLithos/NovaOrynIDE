using NovaOryn.Architecture.X64;
using NovaOryn.Console.Framebuffer;
using NovaOryn.Console.Serial;
using NovaOryn.Interrupts;

namespace NovaOryn.Kernel.Sample;

/// <summary>Mirrors exception diagnostics to the serial and framebuffer consoles.</summary>
internal sealed class KernelDiagnosticSink : IExceptionDiagnosticSink
{
    private readonly SerialConsole serial;
    private readonly FramebufferConsole framebuffer;

    internal KernelDiagnosticSink(SerialConsole serialConsole, FramebufferConsole framebufferConsole)
    {
        serial = serialConsole;
        framebuffer = framebufferConsole;
    }

    public bool WriteLine(string text)
    {
        if (!serial.WriteLine(text)) return false;
        return framebuffer.WriteLine(text);
    }

    public bool StopProcessor() => CPU.Halt();
}
