
using Device.Net;
using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json;

public class Santroller : ConfigurableUSBDevice
{
    private const byte READ_CONFIG_COMMAND = 57;
    public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
        new(0x1209, 0x2882, label: "Santroller",
            classGuid: ControllerGUID);

    public Santroller(PlatformIO pio, IDevice device, string product, string serial, ushort revision) : base(device, product, serial, revision)
    {
        var data = this.ReadData(0x21, READ_CONFIG_COMMAND, 2048);
        using (var inputStream = new MemoryStream(data))
        {
            using (var outputStream = new MemoryStream())
            {

                using (var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(outputStream);
                    _config = JsonSerializer.Deserialize<DeviceConfiguration>(Encoding.UTF8.GetString(outputStream.ToArray()), DeviceConfiguration.generateOptions());
                }
            }
        }
    }
    public override bool MigrationSupported => true;


    private DeviceConfiguration? _config;

    public override DeviceConfiguration? Configuration => _config;

    public override String ToString()
    {
        return $"Santroller";
    }
}