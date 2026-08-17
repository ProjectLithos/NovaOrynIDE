using System;
using System.Runtime.InteropServices;

namespace NovaOryn.Kernel.Internal.X64;

/// <summary>Contains the private x64 native ABI used by managed kernel services.</summary>
public static class Native
{
    /// <summary>Initializes the x64 COM1 serial device used by the managed console.</summary>
    public static Boolean InitializeSerial()
    {
        if (!WritePort8(0x3F9, 0x00)) return false;
        if (!WritePort8(0x3FB, 0x80)) return false;
        if (!WritePort8(0x3F8, 0x01)) return false;
        if (!WritePort8(0x3F9, 0x00)) return false;
        if (!WritePort8(0x3FB, 0x03)) return false;
        if (!WritePort8(0x3FA, 0xC7)) return false;
        return WritePort8(0x3FC, 0x0B);
    }

    /// <summary>Writes one byte to the initialized x64 COM1 serial device.</summary>
    public static Boolean WriteSerial(Byte value) => WritePort8(0x3F8, value);

    /// <summary>Installs the bootstrap processor GDT and TSS.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64InitializeBootstrapDescriptors", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean InitializeBootstrapDescriptors();

    /// <summary>Installs the bootstrap processor IDT.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64InitializeBootstrapInterrupts", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean InitializeBootstrapInterrupts();

    /// <summary>Masks the two legacy 8259 PIC controllers.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64DisableLegacyPic", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean DisableLegacyPic();


    /// <summary>Reads the active x64 CR3 page-table root physical address.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadPageTableRoot", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt64 ReadPageTableRoot();

    /// <summary>Loads one 4 KiB-aligned physical page-table root into x64 CR3.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WritePageTableRoot", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WritePageTableRoot(UInt64 physicalAddress);

    /// <summary>Invalidates the current processor translation for one virtual address.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64InvalidatePage", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean InvalidatePage(UInt64 virtualAddress);

    /// <summary>Enables the x64 EFER.NXE facility when the processor reports execute-disable support.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnableExecuteDisable", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnableExecuteDisable();

    /// <summary>Determines whether the processor supports 1 GiB x64 page-table leaves.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64Supports1GiBPages", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean Supports1GiBPages();



    /// <summary>Reads one x64 model-specific register.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadMsr", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt64 ReadModelSpecificRegister(UInt32 register);

    /// <summary>Gets the APIC identifier of the processor executing the current code.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64GetCurrentApicId", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt32 GetCurrentApicId();

    /// <summary>Copies and patches the relocatable x64 application-processor startup trampoline.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64PrepareApplicationProcessorTrampoline", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean PrepareApplicationProcessorTrampoline(UInt64 trampolineAddress, UInt64 pageTableRoot, UInt64 stackTop);

    /// <summary>Gets the AP startup handshake state stored by the low-memory trampoline.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64GetApplicationProcessorStartupStatus", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt32 GetApplicationProcessorStartupStatus(UInt64 trampolineAddress);

    /// <summary>Gets the APIC identifier observed by the application processor in the startup trampoline.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64GetApplicationProcessorObservedApicId", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt32 GetApplicationProcessorObservedApicId(UInt64 trampolineAddress);

    /// <summary>Reads the invariant-capable x64 timestamp counter.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadTimestampCounter", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt64 ReadTimestampCounter();

    /// <summary>Determines whether CPUID advertises a timestamp counter.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64SupportsTsc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean SupportsTsc();

    /// <summary>Gets whether CPUID advertises an invariant timestamp counter.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64SupportsInvariantTsc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean SupportsInvariantTsc();

    /// <summary>Reads one 32-bit memory-mapped I/O register.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadMmio32", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt32 ReadMmio32(UInt64 address);

    /// <summary>Writes one 32-bit memory-mapped I/O register.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WriteMmio32", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WriteMmio32(UInt64 address, UInt32 value);

    /// <summary>Reads one 64-bit memory-mapped I/O register.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadMmio64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern UInt64 ReadMmio64(UInt64 address);

    /// <summary>Writes one 64-bit memory-mapped I/O register.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WriteMmio64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WriteMmio64(UInt64 address, UInt64 value);

    /// <summary>Initializes a fresh x64 kernel-thread context.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64InitializeThreadContext", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean InitializeThreadContext(UInt64 contextAddress, UInt64 stackTop, UInt64 entryPoint, UInt64 argument);

    /// <summary>Saves the current x64 thread context and transfers execution to another saved context.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64SwitchThreadContext", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean SwitchThreadContext(UInt64 currentContextAddress, UInt64 nextContextAddress);

    /// <summary>Gets whether EFER.NXE is enabled for execute-disable page protections.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64IsExecuteDisableEnabled", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean IsExecuteDisableEnabled();

    /// <summary>Enables CR0.WP so supervisor writes obey read-only page protections.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnableKernelWriteProtect", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnableKernelWriteProtect();

    /// <summary>Gets whether CR0.WP is enabled.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64IsKernelWriteProtectEnabled", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean IsKernelWriteProtectEnabled();

    /// <summary>Gets whether CPUID reports supervisor-mode execution prevention.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64SupportsSmep", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean SupportsSmep();

    /// <summary>Enables CR4.SMEP when supported.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnableSmep", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnableSmep();

    /// <summary>Gets whether CPUID reports supervisor-mode access prevention.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64SupportsSmap", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean SupportsSmap();

    /// <summary>Configures x64 SYSCALL/SYSRET and the current processor syscall stack state.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ConfigureSystemCalls", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean ConfigureSystemCalls(UInt64 stateAddress, UInt64 kernelStackTop);

    /// <summary>Enables CR4.SMAP when the processor supports supervisor-mode access prevention.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnableSmap", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnableSmap();

    /// <summary>Gets whether CR4.SMAP is enabled.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64IsSmapEnabled", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean IsSmapEnabled();

    /// <summary>Temporarily permits supervisor access to validated user pages when SMAP is active.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64BeginUserMemoryAccess", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean BeginUserMemoryAccess();

    /// <summary>Restores SMAP protection after a guarded user-memory copy.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EndUserMemoryAccess", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EndUserMemoryAccess();

    /// <summary>Enters x64 ring 3 using a validated user RIP, stack pointer, and first argument.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnterUserMode", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnterUserMode(UInt64 entryPoint, UInt64 stackTop, UInt64 argument);


    /// <summary>Installs NovaOryn's freestanding managed interrupt dispatcher.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64InstallManagedInterruptDispatcher", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean InstallManagedInterruptDispatcher();

    /// <summary>Enables maskable x64 interrupts.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64EnableInterrupts", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean EnableInterrupts();

    /// <summary>Halts until one interrupt arrives, then returns to managed code.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WaitForInterrupt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WaitForInterrupt();

    /// <summary>Executes one x64 PAUSE hint and returns to managed code.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64Pause", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean Pause();

    /// <summary>Stops the current processor permanently.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64Halt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean Halt();

    /// <summary>Writes one byte to an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WritePort8", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WritePort8(UInt16 port, Byte value);

    /// <summary>Reads one byte from an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadPort8", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean ReadPort8(UInt16 port, out Byte value);

    /// <summary>Writes one 16-bit value to an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WritePort16", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WritePort16(UInt16 port, UInt16 value);

    /// <summary>Reads one 16-bit value from an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadPort16", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean ReadPort16(UInt16 port, out UInt16 value);

    /// <summary>Writes one 32-bit value to an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64WritePort32", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean WritePort32(UInt16 port, UInt32 value);

    /// <summary>Reads one 32-bit value from an x64 I/O port.</summary>
    [DllImport("*", EntryPoint = "NovaOrynX64ReadPort32", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern Boolean ReadPort32(UInt16 port, out UInt32 value);
}
