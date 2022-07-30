
using Device.Net;
using Usb.Net;
using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;

public class Santroller : ConfigurableUSBDevice
{
    public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
        new(0x1209, 0x2882, label: "Santroller",
            classGuid: ControllerGUID);

    public Santroller(PlatformIO pio, IDevice device, string product, string serial, ushort revision) : base(device, product, serial, revision)
    {
        // Note that after we pull initial information, we can actually close or reinitialize the device if needed.
    }
    public override bool MigrationSupported => true;


    private DeviceConfiguration? _config;

    public override DeviceConfiguration? Configuration => _config;

    public override String ToString()
    {
        return $"Santroller";
    }
}