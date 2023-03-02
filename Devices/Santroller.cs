using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using GuitarConfigurator.NetCore.Configuration;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using ProtoBuf;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

public class Santroller : IConfigurableDevice
{
    public static readonly Guid ControllerGuid = new("DF59037D-7C92-4155-AC12-7D700A313D78");
    private readonly Dictionary<int, int> _analogRaw = new();
    private readonly Dictionary<int, bool> _digitalRaw = new();
    private DeviceControllerType? _deviceControllerType;
    private bool _picking;
    private SantrollerUsbDevice? _usbDevice;
    private SerialPort? _serialPort;
    private PlatformIoPort? _platformIoPort;
    private Version Version { get; }
    private Board Board { get; set; }
    private Microcontroller _microcontroller;
    public bool Valid { get; }
    public string RfId { get; private set; }

    public string Serial { get; private set; }


    // public static readonly FilterDeviceDefinition SantrollerDeviceFilter =
    //     new(0x1209, 0x2882, label: "Santroller",
    //         classGuid: ControllerGUID);

    public Santroller(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort version)
    {
        Version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, version & 0xf);
        _usbDevice = new SantrollerUsbDevice(device, path, product, serial, version);
        RfId = "";
        Serial = "";
        _microcontroller = new Pico(Board.Generic);
        Load();
        _usbDevice.SetBoard(Board);
#if Windows
        var versionBytes = ReadData(0, (byte)Commands.CommandReadVersion, 3);
        Version = new Version(versionBytes[0], versionBytes[1], versionBytes[2]);
#endif
    }

    public Santroller(PlatformIo pio, PlatformIoPort port)
    {
        _platformIoPort = port;
        RfId = "";
        Serial = "";
        Version = new Version(1, 0, 0);
        Valid = false;
        _microcontroller = new Pico(Board.Generic);
        try
        {
            _serialPort = new SerialPort(_platformIoPort.Port, 57600, Parity.None, 8, StopBits.One);
            _serialPort.RtsEnable = true;
            _serialPort.DtrEnable = true;
            // Unfortunately, we do need a pretty hefty timeout, as the pro minis bootloader takes a bit
            _serialPort.ReadTimeout = 6000;
            _serialPort.WriteTimeout = 100;
            _serialPort.Open();
            // Santroller devices announce themselves over serial to make it easier to detect them.
            // Sometimes, there will be an extra null byte at the start of transmission, so we need to strip that out
            var line = _serialPort.ReadLine().Replace("\0", "").Trim();
            if (line != "Santroller")
            {
                return;
            }

            Version = Version.Parse(_serialPort.ReadLine().Trim());
            Load();
            Valid = true;
        }
        catch (TimeoutException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }


    private byte[] ReadData(ushort wValue, byte bRequest, ushort size = 128)
    {
        if (_usbDevice != null)
        {
            return _usbDevice.ReadData(wValue, bRequest, size);
        }

        if (_serialPort == null) return Array.Empty<byte>();
        _serialPort.Write(new[] { (byte)0x1f, bRequest, (byte)(wValue & 0xFF), (byte)((wValue << 8) & 0xFF) }, 0,
            4);
        var size2 = _serialPort.ReadByte();
        if (size2 > size) return Array.Empty<byte>();
        var buffer = new byte[size2];
        var read = 0;
        while (read < buffer.Length)
        {
            read += _serialPort.Read(buffer, read, buffer.Length - read);
        }

        return buffer;
    }


    private void WriteData(ushort wValue, byte bRequest, byte[] buffer)
    {
        if (_usbDevice != null)
        {
            _usbDevice.WriteData(wValue, bRequest, buffer);
        }
        else if (_serialPort != null)
        {
            _serialPort.Write(new[] { (byte)0x1e, bRequest }, 0, 1);
            _serialPort.Write(new[] { (byte)buffer.Length }, 0, 1);
            _serialPort.Write(buffer, 0, buffer.Length);
        }
    }

    private void Load()
    {
        var fCpuStr = Encoding.UTF8.GetString(ReadData(0, (byte)Commands.CommandReadFCpu, 32)).Replace("\0", "")
            .Replace("L", "").Trim();
        var fCpu = uint.Parse(fCpuStr);
        var board = Encoding.UTF8.GetString(ReadData(0, (byte)Commands.CommandReadBoard, 32)).Replace("\0", "");
        var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
        Board = m.Board;
        _microcontroller = m;
        RfId = GenerateRfId();
    }

    public bool MigrationSupported => true;

    public bool IsSameDevice(PlatformIoPort port)
    {
        return _platformIoPort == port;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return _usbDevice?.IsSameDevice(serialOrPath) == true;
    }

    public void Bootloader()
    {
        _serialPort?.Close();
        _usbDevice?.Bootloader();
    }

    public void BootloaderUsb()
    {
        _serialPort?.Close();
        if (!Board.HasUsbmcu) return;
        _usbDevice?.BootloaderUsb();
    }

    public bool DeviceAdded(IConfigurableDevice device)
    {
        return _usbDevice != null && _usbDevice.DeviceAdded(device);
    }

    private bool IsOpen()
    {
        if (_usbDevice != null)
        {
            return _usbDevice.IsOpen();
        }

        return _serialPort is { IsOpen: true };
    }

    private async Task TickAsync(ConfigViewModel model)
    {
        while (IsOpen())
        {
            if (_picking)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50));
                continue;
            }

            try
            {
                var direct = model.Bindings.Where(s => s.Input != null).Select(s => s.Input!.InnermostInput())
                    .OfType<DirectInput>().ToList();
                var digital = direct.Where(s => !s.IsAnalog).SelectMany(s => s.Pins);
                var analog = direct.Where(s => s.IsAnalog).SelectMany(s => s.Pins);
                var ports = model.Microcontroller.GetPortsForTicking(digital);
                foreach (var (port, mask) in ports)
                {
                    var wValue = (ushort)(port | (mask << 8));
                    var data = ReadData(wValue, (byte)Commands.CommandReadDigital, sizeof(byte));
                    if (data.Length == 0) return;

                    var pins = data[0];
                    model.Microcontroller.PinsFromPortMask(port, mask, pins, _digitalRaw);
                }

                foreach (var devicePin in analog)
                {
                    var mask = model.Microcontroller.GetAnalogMask(devicePin);
                    var wValue = (ushort)(model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
                    var val = BitConverter.ToUInt16(ReadData(wValue, (byte)Commands.CommandReadAnalog,
                        sizeof(ushort)));
                    _analogRaw[devicePin.Pin] = val;
                }

                var ps2Raw = ReadData(0, (byte)Commands.CommandReadPs2, 9);
                var wiiRaw = ReadData(0, (byte)Commands.CommandReadWii, 8);
                var djLeftRaw = ReadData(0, (byte)Commands.CommandReadDjLeft, 3);
                var djRightRaw = ReadData(0, (byte)Commands.CommandReadDjRight, 3);
                var gh5Raw = ReadData(0, (byte)Commands.CommandReadGh5, 2);
                var ghWtRaw = ReadData(0, (byte)Commands.CommandReadGhWt, sizeof(int));
                var ps2ControllerType = ReadData(0, (byte)Commands.CommandGetExtensionPs2, 1);
                var wiiControllerType = ReadData(0, (byte)Commands.CommandGetExtensionWii, sizeof(short));
                foreach (var output in model.Bindings)
                    output.Update(model.Bindings.ToList(), _analogRaw, _digitalRaw, ps2Raw, wiiRaw, djLeftRaw,
                        djRightRaw, gh5Raw,
                        ghWtRaw, ps2ControllerType, wiiControllerType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // Serial port is slow
            if (_serialPort != null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    private async Task LoadConfigurationAsync(ConfigViewModel model)
    {
        ushort start = 0;
        var data = new List<byte>();
        while (true)
        {
            var chunk = ReadData(start, (byte)Commands.CommandReadConfig, 64);
            if (!chunk.Any()) break;
            data.AddRange(chunk);
            start += 64;
        }

        using var inputStream = new MemoryStream(data.ToArray());
        await using var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress);
        try
        {
            Serializer.Deserialize<SerializedConfiguration>(decompressor).LoadConfiguration(model);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        _deviceControllerType = model.DeviceType;
        // }
        // catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        // {
        //     Console.WriteLine(ex);
        //     throw new NotImplementedException(
        //         "Configuration missing from Santroller device, are you sure this is a real santroller device?");
        //     // TODO: throw a better exception here, and handle this in the gui, so that a device that appears to be missing its config doesn't do something weird.
        // }

        StartTicking(model);
    }

    public bool LoadConfiguration(ConfigViewModel model)
    {
        _ = LoadConfigurationAsync(model);
        return true;
    }

    public Task<string?> GetUploadPortAsync()
    {
        if (_platformIoPort != null)
        {
            _serialPort?.Close();
            return Task.FromResult((string?)_platformIoPort.Port);
        }

        return _usbDevice != null ? _usbDevice.GetUploadPortAsync() : Task.FromResult<string?>(null);
    }

    public bool IsAvr()
    {
        return Board.IsAvr();
    }

    public bool IsGeneric()
    {
        return Board.IsGeneric();
    }

    public bool IsMini()
    {
        return Board.IsMini();
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public string GenerateRfId()
    {
        var data = ReadData(0, (byte)Commands.CommandReadSerial, 20);
        Serial = Encoding.UTF8.GetString(data);
        using SHA256 sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(data);
        return "0x" + BitConverter.ToUInt64(bytes).ToString("X");
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return _microcontroller;
    }

    public void StartTicking(ConfigViewModel model)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() => TickAsync(model));
    }

    public void CancelDetection()
    {
        _picking = false;
    }

    public async Task<int> DetectPinAsync(bool analog, int original, Microcontroller microcontroller)
    {
        _picking = true;
        var importantPins = new List<int>();
        foreach (var config in microcontroller.PinConfigs)
            switch (config)
            {
                case SpiConfig spi:
                    importantPins.AddRange(spi.Pins);
                    break;
                case TwiConfig twi:
                    importantPins.AddRange(twi.Pins);
                    break;
                case DirectPinConfig direct:
                    if (!direct.Type.Contains("-")) importantPins.AddRange(direct.Pins);

                    break;
            }

        if (analog)
        {
            var pins = microcontroller.AnalogPins.Except(importantPins).ToList();
            var analogVals = new Dictionary<int, int>();
            while (_picking)
            {
                foreach (var pin in pins)
                {
                    var devicePin = new DevicePin(pin, DevicePinMode.PullUp);
                    var mask = microcontroller.GetAnalogMask(devicePin);
                    var wValue = (ushort)(microcontroller.GetChannel(pin, true) | (mask << 8));
                    var val = BitConverter.ToUInt16(ReadData(wValue, (byte)Commands.CommandReadAnalog,
                        sizeof(ushort)));
                    if (analogVals.ContainsKey(pin))
                    {
                        var diff = Math.Abs(analogVals[pin] - val);
                        if (diff > 1000)
                        {
                            _picking = false;
                            return pin;
                        }
                    }

                    analogVals[pin] = val;
                }

                await Task.Delay(100);
            }

            return original;
        }

        var allPins = microcontroller.GetAllPins(false).Except(importantPins)
            .Select(s => new DevicePin(s, DevicePinMode.PullUp));
        var ports = microcontroller.GetPortsForTicking(allPins);

        Dictionary<int, byte> tickedPorts = new();
        while (_picking)
        {
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort)(port | (mask << 8));
                var pins = (byte)(ReadData(wValue, (byte)Commands.CommandReadDigital, sizeof(byte))[0] & mask);
                if (tickedPorts.ContainsKey(port))
                    if (tickedPorts[port] != pins)
                    {
                        Dictionary<int, bool> outPins = new();
                        // Xor the old and new values to work out what changed, and then return the first changed bit
                        // Note that we also need to invert this, as pinsFromPortMask is configured assuming a pull up is in place,
                        // Which would then be expecting a zero for a active pin and a 1 for a inactive pin.
                        microcontroller.PinsFromPortMask(port, mask, (byte)~(pins ^ tickedPorts[port]), outPins);
                        _picking = false;
                        return outPins.First(s => s.Value).Key;
                    }

                tickedPorts[port] = pins;
            }

            await Task.Delay(100);
        }

        return original;
    }

    public override string ToString()
    {
        var ret = $"Santroller - {Board.Name} - {Version}";
        if (_deviceControllerType != null) ret += $" - {_deviceControllerType}";

        return ret;
    }

    public enum Commands
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
        CommandSetDetect,
        CommandReadVersion,
        CommandReadSerial
    }

    public string GetSerialPort()
    {
        return _platformIoPort?.Port ?? "";
    }
}