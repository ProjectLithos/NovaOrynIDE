using NovaOryn.Memory.Heap;

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("[FAIL] " + name);
    Console.WriteLine("[ OK ] " + name);
}

unsafe
{
    byte* earlyBacking = stackalloc byte[4096];
    ulong earlyBase = (ulong)(nuint)earlyBacking;
    BumpEarlyAllocator bump = new(earlyBase, 4096);
    Assert(bump.TryAllocate(24, 16, out ulong a) && a >= earlyBase && a % 16 == 0, "Early bump allocator aligns first allocation.");
    Assert(bump.TryAllocate(16, 64, out ulong b) && b > a && b % 64 == 0, "Early bump allocator aligns subsequent allocation.");

    byte* heapBacking = stackalloc byte[65536];
    for (int i = 0; i < 65536; i++) heapBacking[i] = 0xA5;
    ulong heapBase = (ulong)(nuint)heapBacking;
    FirstFitKernelHeap heap = new(heapBase, 65536, 32);
    Assert(heap.TryAllocate(KernelHeapAllocationRequest.Default(128), out KernelHeapAllocation first), "First-fit heap allocates raw storage.");
    bool zeroed = true;
    for (ulong i = 0; i < first.ByteCount; i++) if (*((byte*)(nuint)(first.Address + i)) != 0) { zeroed = false; break; }
    Assert(zeroed, "First-fit heap honors zero-fill requests.");
    Assert(heap.TryAllocate(new KernelHeapAllocationRequest(256, 64, false), out KernelHeapAllocation second) && second.Address % 64 == 0, "First-fit heap honors power-of-two alignment.");
    Assert(heap.TryRelease(first), "First-fit heap releases exact allocation.");
    Assert(!heap.TryRelease(first), "First-fit heap rejects double release.");
    Assert(heap.TryRelease(second), "First-fit heap releases second allocation.");
    KernelHeapStatistics stats = heap.GetStatistics();
    Assert(stats.AllocatedBytes == 0 && stats.FreeBytes == stats.CommittedBytes, "First-fit heap coalesces all released space.");
}

Console.WriteLine("[ OK ] Early allocator and kernel-heap methodology tests passed.");
