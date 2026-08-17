using NovaOryn.Interrupts;

namespace NovaOryn.Architecture.X64.Interrupts;

/// <summary>Allocates x64 driver vectors from 0x40 through 0xEF.</summary>
public sealed class X64InterruptVectorAllocator : IInterruptVectorAllocator
{
    private const byte First = 0x40;
    private const byte Last = 0xEF;
    private readonly bool[] allocated = new bool[256];

    /// <inheritdoc />
    public byte Allocate()
    {
        for (int vector = First; vector <= Last; vector++)
        {
            if (allocated[vector]) continue;
            allocated[vector] = true;
            return (byte)vector;
        }
        return 0;
    }

    /// <inheritdoc />
    public bool Release(byte vector)
    {
        if (vector < First || vector > Last || !allocated[vector]) return false;
        allocated[vector] = false;
        return true;
    }

    /// <inheritdoc />
    public bool IsAllocated(byte vector) => allocated[vector];
}
