using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Processes;
using NovaOryn.Kernel.InterruptDispatch;
using NovaOryn.Kernel.TimerDispatch;
#if NOVAORYN_KERNELAREA_INPUT
using NovaOryn.Kernel.Ps2;
#endif
#if NOVAORYN_KERNELAREA_DRIVERS
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Pci;
using NovaOryn.Kernel.InterruptBroker;
using NovaOryn.Kernel.Virtio.Gpu;
using NovaOryn.Kernel.Graphics;
#endif
#if NOVAORYN_KERNELAREA_STORAGE
using NovaOryn.Kernel.Storage;
using NovaOryn.Kernel.Nvme;
using NovaOryn.Kernel.Ahci;
#endif
#if NOVAORYN_KERNELAREA_NETWORKING
using NovaOryn.Kernel.Networking;
using NovaOryn.Kernel.Virtio;
using NovaOryn.Kernel.E1000;
using NovaOryn.Kernel.Rtl8168;
#endif
#if NOVAORYN_KERNELAREA_USB
using NovaOryn.Bus.Usb;
using NovaOryn.Usb.Xhci;
using NovaOryn.Usb.Hid;
using NovaOryn.Usb.MassStorage;
using NovaOryn.Usb.Hub;
#endif

namespace NovaOryn.Kernel.Bootstrap.HAL;

/// <summary>Initializes only hardware/service families assigned to the kernel by the authoritative NovaOryn configuration.</summary>
public static unsafe class HardwareAbstractionLayer
{
    /// <summary>Starts configured kernel-domain mechanisms and leaves userland-domain services for process/service startup.</summary>
    public static Boolean Initialize()
    {
        if (!KernelProcesses.Initialize()) return false;
        if (!KernelInterruptDispatch.Initialize()) return false;
        if (!KernelTimerDispatch.Initialize()) return false;

#if NOVAORYN_KERNEL_MICROKERNEL
        if (!KernelConsole.WriteLine("Microkernel topology: optional drivers, storage, networking, USB, audio, GUI and filesystem services are excluded from HAL kernel startup.")) return false;
#endif
#if NOVAORYN_KERNEL_HYBRID
        if (!KernelConsole.WriteLine("Hybrid topology: only work areas assigned to the Kernel execution domain are initialized by HAL.")) return false;
#endif
#if NOVAORYN_KERNEL_MONOLITHIC
        if (!KernelConsole.WriteLine("Monolithic topology: selected kernel-domain work areas are initialized directly by HAL.")) return false;
#endif

#if NOVAORYN_KERNELAREA_INPUT
        if (!KernelPs2.Initialize()) return false;
        Ps2Capabilities ps2 = KernelPs2.GetCapabilities();
        if (!KernelConsole.Write("PS/2 keyboard/mouse: ")) return false;
        if (!KernelConsole.Write(ps2.Keyboard ? "keyboard" : "no keyboard")) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteLine(ps2.Mouse ? "mouse" : "no mouse")) return false;
#endif

#if NOVAORYN_KERNELAREA_DRIVERS
        if (!KernelDrivers.Initialize()) return false;
        if (!KernelPci.Initialize()) return false;
        if (!KernelInterruptBroker.Initialize()) return false;
        if (!KernelVirtioGpu.Initialize()) return false;
        KernelDriverCapabilities drivers = KernelDrivers.GetCapabilities();
        PciCapabilities pci = KernelPci.GetCapabilities();
        VirtioGpuCapabilities virtioGpu = KernelVirtioGpu.GetCapabilities();
        KernelGraphicsCapabilities graphics = KernelGraphics.GetCapabilities();
        if (!KernelConsole.Write("Kernel driver framework drivers/devices: ")) return false;
        if (!KernelConsole.WriteUInt64(drivers.RegisteredDrivers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(drivers.RegisteredDevices)) return false;
        if (!KernelConsole.Write("; PCI devices: ")) return false;
        if (!KernelConsole.WriteUInt64(pci.DeviceCount)) return false;
        if (!KernelConsole.WriteLine("")) return false;
        if (!KernelConsole.Write("VirtIO GPU controllers/displays: ")) return false;
        if (!KernelConsole.WriteUInt64(virtioGpu.Controllers)) return false;
        if (!KernelConsole.Write(" / ")) return false;
        if (!KernelConsole.WriteUInt64(virtioGpu.Displays)) return false;
        if (!KernelConsole.Write("; graphics displays total: ")) return false;
        if (!KernelConsole.WriteUInt64(graphics.Displays)) return false;
        if (!KernelConsole.WriteLine("")) return false;
#endif

#if NOVAORYN_KERNELAREA_STORAGE
        if (!KernelStorage.Initialize()) return false;
        if (!KernelNvme.Initialize()) return false;
        if (!KernelAhci.Initialize()) return false;
        if (!KernelConsole.WriteLine("Kernel-domain storage services online.")) return false;
#endif

#if NOVAORYN_KERNELAREA_NETWORKING
        if (!KernelNetworking.Initialize()) return false;
        if (!KernelVirtio.Initialize()) return false;
        if (!KernelE1000.Initialize()) return false;
        if (!KernelRtl8168.Initialize()) return false;
        if (!KernelConsole.WriteLine("Kernel-domain networking services online.")) return false;
#endif

#if NOVAORYN_KERNELAREA_USB
        if (!KernelXhci.Initialize()) return false;
        if (!KernelXhci.ScanRootPorts()) return false;
        if (!UsbHub.Initialize()) return false;
        if (!UsbHub.EnumerateDownstream()) return false;
        if (!UsbHid.Initialize()) return false;
        if (!UsbMassStorage.Initialize()) return false;
        if (!KernelConsole.WriteLine("Kernel-domain USB services online.")) return false;
#endif

        return true;
    }
}
