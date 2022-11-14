using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;
using ProtoBuf;
using ReactiveUI;

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
        CommandReadDigital,
        CommandReadAnalog,
        CommandReadPs2,
        CommandReadWii,
        CommandReadDjLeft,
        CommandReadDjRight,
        CommandReadGh5,
        CommandReadGhWt,
        CommandGetExtensionWii,
        CommandGetExtensionPs2,
        CommandSetLeds,
    }

    public override bool MigrationSupported => true;
    public static readonly Guid ControllerGuid = new("DF59037D-7C92-4155-AC12-7D700A313D78");
    private DeviceControllerType? _deviceControllerType;
    private Dictionary<int, bool> _digitalRaw = new();
    private Dictionary<int, int> _analogRaw = new();

    // public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
    //     new(0x1209, 0x2882, label: "Santroller",
    //         classGuid: ControllerGUID);

    public Santroller(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort revision) :
        base(device, path, product, serial, revision)
    {
        var fCpuStr = Encoding.UTF8.GetString(ReadData(0, ((byte) Commands.CommandReadFCpu), 32)).Replace("\0", "")
            .Replace("L", "").Trim();
        var fCpu = uint.Parse(fCpuStr);
        var board = Encoding.UTF8.GetString(ReadData(0, ((byte) Commands.CommandReadBoard), 32)).Replace("\0", "");
        var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
        Board = m.Board;
    }

    public override void Bootloader()
    {
        WriteData(0, ((byte) Commands.CommandJumpBootloader), Array.Empty<byte>());
        Device.Close();
    }

    public override void BootloaderUsb()
    {
        if (!Board.HasUsbmcu) return;
        WriteData(0, ((byte) Commands.CommandJumpBootloaderUno), Array.Empty<byte>());
        Device.Close();
    }

    private async Task Tick(ConfigViewModel model)
    {
        while (Device.IsOpen)
        {
            var direct = model.Bindings.Where(s => s.Input != null).Select(s => s.Input!.InnermostInput())
                .OfType<DirectInput>().ToList();
            var digital = direct.Where(s => !s.IsAnalog).SelectMany(s => s.Pins);
            var analog = direct.Where(s => s.IsAnalog).SelectMany(s => s.Pins);
            var ports = model.MicroController!.GetPortsForTicking(digital);
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var pins = ReadData(wValue, (byte) Commands.CommandReadDigital, sizeof(byte))[0];
                model.MicroController!.PinsFromPortMask(port, mask, pins, _digitalRaw);
            }

            foreach (var devicePin in analog)
            {
                var mask = model.MicroController!.GetAnalogMask(devicePin);
                var wValue = (ushort) (devicePin.Pin | (mask << 8));
                var val = BitConverter.ToUInt16(ReadData(wValue, (byte) Commands.CommandReadAnalog,
                    sizeof(ushort)));
                _analogRaw[devicePin.Pin] = val;
            }

            var ps2Raw = ReadData(0, (byte) Commands.CommandReadPs2, 32);
            var wiiRaw = ReadData(0, (byte) Commands.CommandReadWii, 8);
            var djLeftRaw = ReadData(0, (byte) Commands.CommandReadDjLeft, 3);
            var djRightRaw = ReadData(0, (byte) Commands.CommandReadDjRight, 3);
            var gh5Raw = ReadData(0, (byte) Commands.CommandReadGh5, 2);
            var ghWtRawRaw = ReadData(0, (byte) Commands.CommandReadGhWt, sizeof(int));
            var ps2ControllerType = ReadData(0, (byte) Commands.CommandGetExtensionPs2, 1);
            var wiiControllerType = ReadData(0, (byte) Commands.CommandGetExtensionWii, sizeof(short));
            var ghWtRaw = 0;
            if (ghWtRawRaw.Any())
            {
                ghWtRaw = BitConverter.ToInt32(ghWtRawRaw);
            }
            foreach (var output in model.Bindings)
            {
               output.Update(_analogRaw, _digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                        ghWtRaw, ps2ControllerType, wiiControllerType);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    public override async Task LoadConfiguration(ConfigViewModel model)
    {
        try
        {
            var fCpuStr = Encoding.UTF8.GetString(ReadData(0, ((byte) Commands.CommandReadFCpu), 32)).Replace("\0", "")
                .Replace("L", "").Trim();
            var fCpu = uint.Parse(fCpuStr);
            var board = Encoding.UTF8.GetString(ReadData(0, ((byte) Commands.CommandReadBoard), 32)).Replace("\0", "");
            var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
            Board = m.Board;
            model.MicroController = m;
            var data = ReadData(0, ((byte) Commands.CommandReadConfig), 2048);
            using var inputStream = new MemoryStream(data);
            await using var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress);
            Serializer.Deserialize<SerializedConfiguration>(decompressor).LoadConfiguration(model);
            _deviceControllerType = model.DeviceType;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            Console.WriteLine(ex);
            throw new NotImplementedException(
                "Configuration missing from Santroller device, are you sure this is a real santroller device?");
            // TODO: throw a better exception here, and handle this in the gui, so that a device that appears to be missing its config doesn't do something weird.
        }

        StartTicking(model);
    }

    public void StartTicking(ConfigViewModel model) {
        _ = Dispatcher.UIThread.InvokeAsync(() => Tick(model));
    }

    public override string ToString()
    {
        var ret = $"Santroller - {Board.Name} - {Version}";
        if (_deviceControllerType != null)
        {
            ret += $" - {_deviceControllerType}";
        }

        return ret;
    }
}