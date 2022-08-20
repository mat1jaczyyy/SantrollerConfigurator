using LibUsbDotNet;
using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.Json;

public class Santroller : ConfigurableUSBDevice
{
    enum Commands : int
    {
        COMMAND_REBOOT = 0x30,
        COMMAND_JUMP_BOOTLOADER,
        COMMAND_JUMP_BOOTLOADER_UNO,
        COMMAND_READ_CONFIG,
        COMMAND_READ_F_CPU,
        COMMAND_READ_BOARD,
        COMMAND_FIND_DIGITAL,
        COMMAND_FIND_ANALOG,
        COMMAND_GET_FOUND,
        COMMAND_GET_EXTENSION,
        COMMAND_SET_LEDS,
        COMMAND_SET_SP,
    }

    public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    // public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
    //     new(0x1209, 0x2882, label: "Santroller",
    //         classGuid: ControllerGUID);

    public Santroller(PlatformIO pio, string path, UsbDevice device, string product, string serial, ushort revision) : base(device, path, product, serial, revision)
    {
        try
        {
            var f_cpu_str = Encoding.UTF8.GetString(this.ReadData(0, ((byte)Commands.COMMAND_READ_F_CPU), 32)).Replace("L", "").Trim();
            var f_cpu = uint.Parse(f_cpu_str);
            var board = Encoding.UTF8.GetString(this.ReadData(0, ((byte)Commands.COMMAND_READ_BOARD), 32)).Replace("\0", "");
            Microcontroller m = Board.findMicrocontroller(Board.findBoard(board, f_cpu));
            this.Board = m.Board;
            var data = this.ReadData(0, ((byte)Commands.COMMAND_READ_CONFIG), 2048);
            using (var inputStream = new MemoryStream(data))
            {
                using (var outputStream = new MemoryStream())
                {

                    using (var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress))
                    {
                        decompressor.CopyTo(outputStream);
                        _config = JsonSerializer.Deserialize<DeviceConfiguration>(Encoding.UTF8.GetString(outputStream.ToArray()), DeviceConfiguration.getJSONOptions(m))!;
                        _config.generate(pio);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is FormatException)
        {
            throw new NotImplementedException("Configuration missing from Santroller device, are you sure this is a real santroller device?");
            // TODO: throw a better exception here, and handle this in the gui, so that a device that appears to be missing its config doesn't do something weird.
        }
    }

    public override void Bootloader()
    {
        WriteData(0, ((byte)Commands.COMMAND_JUMP_BOOTLOADER), new byte[0]);
    }
    public override void BootloaderUSB()
    {
        if (Configuration.MicroController.Board.hasUSBMCU)
        {
            WriteData(0, ((byte)Commands.COMMAND_JUMP_BOOTLOADER_UNO), new byte[0]);
        }
    }
    public override bool MigrationSupported => true;


    private DeviceConfiguration _config;

    public override DeviceConfiguration Configuration => _config;

    public override String ToString()
    {
        return $"Santroller";
    }
}