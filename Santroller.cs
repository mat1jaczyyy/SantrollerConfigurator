using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore;

public class Santroller : ConfigurableUsbDevice
{
    enum Commands
    {
        CommandReboot = 0x30,
        CommandJumpBootloader,
        CommandJumpBootloaderUno,
        CommandReadConfig,
        CommandReadFCpu,
        CommandReadBoard,
        CommandFindDigital,
        CommandFindAnalog,
        CommandGetFound,
        CommandGetExtension,
        CommandSetLeds,
        CommandSetSp,
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
        WriteData(0, ((byte)Commands.CommandJumpBootloader), Array.Empty<byte>());
    }
    public override void BootloaderUsb()
    {
        if (Board.HasUsbmcu)
        {
            WriteData(0, ((byte)Commands.CommandJumpBootloaderUno), Array.Empty<byte>());
        }
    }

    public override void LoadConfiguration(ConfigViewModel model)
    {
        try
        {
            var fCpuStr = Encoding.UTF8.GetString(ReadData(0, ((byte)Commands.CommandReadFCpu), 32)).Replace("\0", "").Replace("L", "").Trim();
            var fCpu = uint.Parse(fCpuStr);
            var board = Encoding.UTF8.GetString(ReadData(0, ((byte)Commands.CommandReadBoard), 32)).Replace("\0", "");
            Microcontroller m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
            Board = m.Board;
            model.MicroController = m;
            var data = ReadData(0, ((byte)Commands.CommandReadConfig), 2048);
            using (var inputStream = new MemoryStream(data))
            {
                using (var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress))
                {
                    Serializer.Deserialize<SerializedConfiguration>(decompressor).LoadConfiguration(model);
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            throw new NotImplementedException("Configuration missing from Santroller device, are you sure this is a real santroller device?");
            // TODO: throw a better exception here, and handle this in the gui, so that a device that appears to be missing its config doesn't do something weird.
        }
    }

    public override string ToString()
    {
        return "Santroller";
    }
}