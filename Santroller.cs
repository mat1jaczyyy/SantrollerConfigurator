
using Device.Net;
using Usb.Net;
using System;
using GuitarConfiguratorSharp.Utils;

public class Santroller : ConfigurableUSBDevice
{
    public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
        new(0x1209, 0x2882, label: "Santroller",
            classGuid: ControllerGUID);

    public Santroller(IDevice device, string product, string serial, ushort revision) : base(device, product, serial, revision)
    {
        // Note that after we pull initial information, we can actually close or reinitialize the device if needed.

    }
    public override bool MigrationSupported => true;

    public override String ToString()
    {
        return $"Santroller";
    }
}