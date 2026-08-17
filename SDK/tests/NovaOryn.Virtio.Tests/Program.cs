using NovaOryn.Kernel.Virtio;

static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}

Assert(VirtioMath.IdentifyDeviceType(0x1041,0)==VirtioDeviceType.Network,"Modern VirtIO network PCI ID is identified.");
Assert(VirtioMath.IdentifyDeviceType(0x1042,0)==VirtioDeviceType.Block,"Modern VirtIO block PCI ID is identified.");
Assert(VirtioMath.IdentifyDeviceType(0x1043,0)==VirtioDeviceType.Console,"Modern VirtIO console PCI ID is identified.");
Assert(VirtioMath.IdentifyDeviceType(0x1044,0)==VirtioDeviceType.EntropySource,"Modern VirtIO RNG PCI ID is identified.");
Assert(VirtioMath.IdentifyDeviceType(0x1000,2)==VirtioDeviceType.Block,"Transitional VirtIO devices use subsystem device type.");
Assert(VirtioMath.SelectQueueSize(256,128)==128,"Virtqueue sizing honors the requested queue size.");
Assert(VirtioMath.SelectQueueSize(96,128)==64,"Virtqueue sizing selects a legal power of two below a non-power device maximum.");
Assert(VirtioMath.SplitQueueBytes(8)==222UL,"Split virtqueue layout includes descriptors, available ring alignment, and used ring.");
Console.WriteLine("[ OK ] VirtIO tests passed.");
