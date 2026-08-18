using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.CommandLine;
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
#if NOVAORYN_KERNELAREA_INPUT
    private static UInt32 _inputTimerHandle;
    private static UInt64 _keyboardIrqHandle;
    private static UInt64 _mouseIrqHandle;
#endif
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
        // The hardware driver decodes scan codes; the HAL bridges decoded events into the
        // common command-line input path. A timer-service drain is always installed so
        // interactive input remains live even on firmware with unusual legacy IRQ routing.
        if (!KernelPs2.SetKeyboardEventHandler(&HandlePs2KeyboardEvent)) return false;
        if (!KernelTimerDispatch.Register(1000000UL, &ServiceInput, 0UL, out _inputTimerHandle)) return false;
#endif

#if NOVAORYN_KERNELAREA_DRIVERS
        if (!KernelDrivers.Initialize()) return false;
        if (!KernelPci.Initialize()) return false;
        if (!KernelInterruptBroker.Initialize()) return false;
#if NOVAORYN_KERNELAREA_INPUT
        Boolean keyboardIrq = KernelInterruptBroker.RegisterLegacyGsi(1U, false, false, &HandlePs2Interrupt, 0UL, out _keyboardIrqHandle);
        Boolean mouseIrq = !KernelPs2.GetCapabilities().Mouse || KernelInterruptBroker.RegisterLegacyGsi(12U, false, false, &HandlePs2Interrupt, 0UL, out _mouseIrqHandle);
        Boolean ps2Irqs = keyboardIrq && mouseIrq && KernelPs2.SetHardwareInterrupts(true);
        if (!KernelConsole.WriteLine(ps2Irqs ? "PS/2 input: hardware IRQ delivery active (timer drain retained as safety net)." : "PS/2 input: timer-dispatch drain active.")) return false;
#endif
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
#if NOVAORYN_KERNELAREA_INPUT
        if (!UsbHid.SetKeyboardEventHandler(&HandleUsbKeyboardEvent)) return false;
#endif
        if (!UsbMassStorage.Initialize()) return false;
        if (!KernelConsole.WriteLine("Kernel-domain USB services online.")) return false;
#endif

        return true;
    }
#if NOVAORYN_KERNELAREA_INPUT
    private static Boolean ServiceInput(UInt64 cookie)
    {
        Boolean ok = KernelPs2.Service();
#if NOVAORYN_KERNELAREA_USB
        if (UsbHid.IsInitialized()) ok = UsbHid.Service() & ok;
#endif
        return ok;
    }

    private static Boolean HandlePs2Interrupt(Byte vector, UInt64 cookie) => KernelPs2.Service();

    private static Boolean HandlePs2KeyboardEvent(Ps2KeyboardEvent input)
    {
        if (!input.Pressed) return true;
        if (input.Key == Ps2Key.Up) return KernelConsole.ScrollUp();
        if (input.Key == Ps2Key.Down) return KernelConsole.ScrollDown();
        if (input.Control && input.Key == Ps2Key.D1) return KernelConsole.SetFramebufferBufferCount(1U);
        if (input.Control && input.Key == Ps2Key.D2) return KernelConsole.SetFramebufferBufferCount(2U);
        if (input.Control && input.Key == Ps2Key.D3) return KernelConsole.SetFramebufferBufferCount(3U);
        if (input.Alt && input.Key == Ps2Key.D1) return KernelConsole.SetFontPreset(1U);
        if (input.Alt && input.Key == Ps2Key.D2) return KernelConsole.SetFontPreset(2U);
        if (input.Alt && input.Key == Ps2Key.D3) return KernelConsole.SetFontPreset(3U);
        return KernelCommandLine.HandleCharacter(input.Character);
    }

#if NOVAORYN_KERNELAREA_USB
    private static Boolean HandleUsbKeyboardEvent(UsbHidKeyboardEvent input)
    {
        if (!input.Pressed) return true;
        if (input.Usage == 82U) return KernelConsole.ScrollUp();
        if (input.Usage == 81U) return KernelConsole.ScrollDown();
        Boolean control = (input.Modifiers & 0x11U) != 0U;
        Boolean alt = (input.Modifiers & 0x44U) != 0U;
        if (control && input.Usage == 30U) return KernelConsole.SetFramebufferBufferCount(1U);
        if (control && input.Usage == 31U) return KernelConsole.SetFramebufferBufferCount(2U);
        if (control && input.Usage == 32U) return KernelConsole.SetFramebufferBufferCount(3U);
        if (alt && input.Usage == 30U) return KernelConsole.SetFontPreset(1U);
        if (alt && input.Usage == 31U) return KernelConsole.SetFontPreset(2U);
        if (alt && input.Usage == 32U) return KernelConsole.SetFontPreset(3U);
        return KernelCommandLine.HandleCharacter(input.Character);
    }
#endif
#endif

}
