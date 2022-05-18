
using Device.Net;
using Usb.Net;
using System;
using GuitarConfiguratorSharp.Utils;

public class Santroller : ConfigurableDevice
{
    public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
        new(0x1209, 0x2882, label: "Santroller",
            classGuid: ControllerGUID);
    private IUsbDevice device;

    public Santroller(IDevice device)
    {
        this.device = (IUsbDevice)device;
        // Note that after we pull initial information, we can actually close or reinitialize the device if needed.

    }

    public bool IsSameDevice(IDevice device)
    {
        return this.device == device;
    }

    public bool IsSameDevice(PlatformIOPort port)
    {
        return false;
    }

    public bool IsSameDevice(string path)
    {
        return false;
    }
    
    public override String ToString()
    {
        return $"Santroller";
    }
}