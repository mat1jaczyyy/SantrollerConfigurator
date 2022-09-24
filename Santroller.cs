using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;

namespace GuitarConfiguratorSharp.NetCore;

public class Santroller : ConfigurableUsbDevice
{
    enum Commands
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

    public override bool MigrationSupported => true;
    public static readonly Guid ControllerGuid = new("DF59037D-7C92-4155-AC12-7D700A313D78");

    // public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
    //     new(0x1209, 0x2882, label: "Santroller",
    //         classGuid: ControllerGUID);

    public Santroller(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort revision) : base(device, path, product, serial, revision)
    {
       
    }

    public override void Bootloader()
    {
        WriteData(0, ((byte)Commands.COMMAND_JUMP_BOOTLOADER), Array.Empty<byte>());
    }
    public override void BootloaderUsb()
    {
        if (Board.HasUsbmcu)
        {
            WriteData(0, ((byte)Commands.COMMAND_JUMP_BOOTLOADER_UNO), Array.Empty<byte>());
        }
    }

    public override void LoadConfiguration(ConfigViewModel model)
    {
        try
        {
            var fCpuStr = Encoding.UTF8.GetString(this.ReadData(0, ((byte)Commands.COMMAND_READ_F_CPU), 32)).Replace("\0", "").Replace("L", "").Trim();
            var fCpu = uint.Parse(fCpuStr);
            var board = Encoding.UTF8.GetString(this.ReadData(0, ((byte)Commands.COMMAND_READ_BOARD), 32)).Replace("\0", "");
            Microcontroller m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
            this.Board = m.Board;
            var data = this.ReadData(0, ((byte)Commands.COMMAND_READ_CONFIG), 2048);
            using (var inputStream = new MemoryStream(data))
            {
                using (var outputStream = new MemoryStream())
                {

                    using (var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress))
                    {
                        decompressor.CopyTo(outputStream);
                        //TODO: how do
                        // _config = JsonSerializer.Deserialize<DeviceConfiguration>(Encoding.UTF8.GetString(outputStream.ToArray()), DeviceConfiguration.GetJsonOptions(m))!;
                        // _config.Generate(pio);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is FormatException || ex is InvalidOperationException)
        {
            throw new NotImplementedException("Configuration missing from Santroller device, are you sure this is a real santroller device?");
            // TODO: throw a better exception here, and handle this in the gui, so that a device that appears to be missing its config doesn't do something weird.
        }
    }

    public override String ToString()
    {
        return $"Santroller";
    }
}