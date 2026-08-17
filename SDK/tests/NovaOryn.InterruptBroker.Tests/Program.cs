using System;
using NovaOryn.Kernel.InterruptBroker;
static void Require(Boolean value,String message){if(!value)throw new InvalidOperationException(message);Console.WriteLine("[ OK ] "+message);}
Require(KernelInterruptBrokerMath.SelectPciMechanism(true,true,true)==KernelInterruptDeliveryMechanism.MsiX,"MSI-X is preferred when available.");
Require(KernelInterruptBrokerMath.SelectPciMechanism(false,true,true)==KernelInterruptDeliveryMechanism.Msi,"MSI is preferred when MSI-X is unavailable.");
Require(KernelInterruptBrokerMath.SelectPciMechanism(false,false,true)==KernelInterruptDeliveryMechanism.IoApic,"I/O APIC INTx is the PCI fallback.");
Require(KernelInterruptBrokerMath.SelectPciMechanism(false,false,false)==KernelInterruptDeliveryMechanism.None,"No route is reported when no delivery mechanism exists.");
Require(KernelInterruptBrokerMath.CreateMsiAddress(2U)==0xFEE02000UL,"MSI address encodes the APIC destination.");
Require(KernelInterruptBrokerMath.CreateMsiData(0x51)==0x51,"MSI data encodes the allocated interrupt vector.");
Console.WriteLine("[ OK ] Opaque interrupt-broker policy tests passed.");
