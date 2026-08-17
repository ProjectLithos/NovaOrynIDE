using System;
using NovaOryn.Bus.Usb;
internal static unsafe class Program
{
    private static Int32 Main()
    {
        Byte[] device={18,1,0x10,0x03,0,0,0,64,0x34,0x12,0x78,0x56,0x00,0x01,1,2,3,1};
        fixed(Byte* p=device)
        {
            if(!UsbDescriptorParser.TryParseDevice(p,(UInt32)device.Length,out UsbDeviceDescriptor d)) return 1;
            if(d.VendorId!=0x1234||d.ProductId!=0x5678||d.MaximumPacketSize0!=64||d.ConfigurationCount!=1) return 2;
        }
        Byte[] inf={9,4,2,0,1,3,1,1,0};
        fixed(Byte* p=inf)
        {
            if(!UsbDescriptorParser.TryParseInterface(p,(UInt32)inf.Length,out UsbInterfaceDescriptor d)) return 3;
            if(d.Class!=(Byte)UsbClassCode.Hid||d.SubClass!=1||d.Protocol!=1||d.EndpointCount!=1) return 4;
        }
        Byte[] ep={7,5,0x81,3,8,0,10};
        fixed(Byte* p=ep)
        {
            if(!UsbDescriptorParser.TryParseEndpoint(p,(UInt32)ep.Length,out UsbEndpointDescriptor d)) return 5;
            if(!d.In||d.Number!=1||d.TransferType!=UsbTransferType.Interrupt||d.MaximumPacketSize!=8) return 6;
        }
        Console.WriteLine("NovaOryn USB descriptor tests passed.");
        return 0;
    }
}
