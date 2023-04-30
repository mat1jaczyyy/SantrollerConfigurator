using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Devices;

public class Santroller : IConfigurableDevice
{
    private readonly Dictionary<int, int> _analogRaw = new();
    private readonly Dictionary<int, bool> _digitalRaw = new();
    private readonly Dictionary<byte, TimeSpan> _ledTimers = new();
    private ConfigViewModel? _model;
    readonly Stopwatch _sw = Stopwatch.StartNew();
    private DispatcherTimer _timer;

    private async void Tick(object? sender, EventArgs e)
    {
        if (_model == null) return;
        if (!IsOpen())
        {
            _timer.Stop();
            return;
        }

        foreach (var (led, elapsed) in _ledTimers)
        {
            if (_sw.Elapsed - elapsed <= TimeSpan.FromSeconds(2)) continue;
            ClearLed(led);
            _ledTimers.Remove(led);
        }

        try
        {
            var direct = _model.Bindings.Items.Select(s => s.Input!.InnermostInput())
                .OfType<DirectInput>().ToList();
            var digital = direct.Where(s => !s.IsAnalog).SelectMany(s => s.Pins);
            var analog = direct.Where(s => s.IsAnalog).SelectMany(s => s.Pins);
            var ports = _model.Microcontroller.GetPortsForTicking(digital);
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var data = await ReadDataAsync(wValue, (byte) Commands.CommandReadDigital, sizeof(byte));
                if (data.Length == 0) return;

                var pins = data[0];
                _model.Microcontroller.PinsFromPortMask(port, mask, pins, _digitalRaw);
            }

            foreach (var devicePin in analog)
            {
                var mask = _model.Microcontroller.GetAnalogMask(devicePin);
                var wValue = (ushort) (_model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
                var val = BitConverter.ToUInt16(await ReadDataAsync(wValue, (byte) Commands.CommandReadAnalog,
                    sizeof(ushort)));
                _analogRaw[devicePin.Pin] = val;
            }

            var ps2Raw = await ReadDataAsync(0, (byte) Commands.CommandReadPs2, 9);
            var wiiRaw = await ReadDataAsync(0, (byte) Commands.CommandReadWii, 8);
            var djLeftRaw = await ReadDataAsync(0, (byte) Commands.CommandReadDjLeft, 3);
            var djRightRaw = await ReadDataAsync(0, (byte) Commands.CommandReadDjRight, 3);
            var gh5Raw = await ReadDataAsync(0, (byte) Commands.CommandReadGh5, 2);
            var ghWtRaw = await ReadDataAsync(0, (byte) Commands.CommandReadGhWt, sizeof(int));
            var ps2ControllerType = await ReadDataAsync(0, (byte) Commands.CommandGetExtensionPs2, 1);
            var wiiControllerType = await ReadDataAsync(0, (byte) Commands.CommandGetExtensionWii, sizeof(short));
            var rfRaw = await ReadDataAsync(0, (byte) Commands.CommandReadRf, 2);
            var usbHostRaw = Array.Empty<byte>();
            if (_model.UsbHostEnabled)
            {
                usbHostRaw = await ReadDataAsync(0, (byte) Commands.CommandReadUsbHost, 24);
            }
            var bluetoothRaw = Array.Empty<byte>();
            if (IsPico())
            {
                bluetoothRaw = await ReadDataAsync(0, (byte) Commands.CommandGetBtState, 1);
            }

            _model.Update(rfRaw, bluetoothRaw);
            foreach (var output in _model.Bindings.Items)
                output.Update(_model.Bindings.Items.ToList(), _analogRaw, _digitalRaw, ps2Raw, wiiRaw, djLeftRaw,
                    djRightRaw, gh5Raw,
                    ghWtRaw, ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private DeviceControllerType? _deviceControllerType;
    private bool _picking;
    private SantrollerUsbDevice? _usbDevice;
    private SerialPort? _serialPort;
    private PlatformIoPort? _platformIoPort;
    private Board Board { get; set; }
    private Microcontroller _microcontroller;
    public bool Valid { get; }

    public string Serial { get; private set; }

    private bool InvalidDevice { get; }

    public Santroller(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort version)
    {
        _usbDevice = new SantrollerUsbDevice(device, path, product, serial, version);
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, Tick);
        Serial = "";
        _microcontroller = new Pico(Board.Generic);
        if (device is IUsbDevice usbDevice)
        {
            usbDevice.ClaimInterface(2);
        }

        Load();
        if (Board.Name == Board.Generic.Name)
        {
            InvalidDevice = true;
            return;
        }

        _usbDevice.SetBoard(Board);
    }

    public Santroller(PlatformIo pio, PlatformIoPort port)
    {
        _platformIoPort = port;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, Tick);
        Serial = "";
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
        _serialPort.Write(new[] {(byte) 0x1f, bRequest, (byte) (wValue & 0xFF), (byte) ((wValue << 8) & 0xFF)}, 0,
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

    private async Task<byte[]> ReadDataAsync(ushort wValue, byte bRequest, ushort size = 128)
    {
        if (_usbDevice != null)
        {
            return _usbDevice.ReadData(wValue, bRequest, size);
        }

        if (_serialPort == null) return Array.Empty<byte>();
        _serialPort.Write(new[] {(byte) 0x1f, bRequest, (byte) (wValue & 0xFF), (byte) ((wValue << 8) & 0xFF)}, 0,
            4);
        var size2 = _serialPort.ReadByte();
        if (size2 > size) return Array.Empty<byte>();
        var buffer = new byte[size2];
        using var memoryStream = new MemoryStream();
        var readBytes = 0;
        while (readBytes < size2)
        {
            var bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, readBytes, buffer.Length - readBytes);
            memoryStream.Write(buffer, 0, bytesRead);
            readBytes += bytesRead;
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
            _serialPort.Write(new[] {(byte) 0x1e, bRequest}, 0, 1);
            _serialPort.Write(new[] {(byte) buffer.Length}, 0, 1);
            _serialPort.Write(buffer, 0, buffer.Length);
        }
    }

    private void Load()
    {
        var fCpuStr = Encoding.UTF8.GetString(ReadData(0, (byte) Commands.CommandReadFCpu, 32)).Replace("\0", "")
            .Replace("L", "").Trim();
        if (!fCpuStr.Any())
        {
            return;
        }

        var fCpu = uint.Parse(fCpuStr);
        var board = Encoding.UTF8.GetString(ReadData(0, (byte) Commands.CommandReadBoard, 32)).Replace("\0", "");
        var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
        Board = m.Board;
        _microcontroller = m;
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

        return _serialPort is {IsOpen: true};
    }

    private async Task LoadConfigurationAsync(ConfigViewModel model)
    {
        ushort start = 0;
        var data = new List<byte>();
        while (true)
        {
            var chunk = await ReadDataAsync(start, (byte) Commands.CommandReadConfig, 64);
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
            Trace.TraceError(ex.StackTrace);
        }

        _deviceControllerType = model.DeviceType;

        _model = model;
        _timer.Start();
    }

    public bool LoadConfiguration(ConfigViewModel model)
    {
        _ = LoadConfigurationAsync(model);
        return true;
    }

    public Task<string?> GetUploadPortAsync()
    {
        if (_platformIoPort == null)
            return _usbDevice != null ? _usbDevice.GetUploadPortAsync() : Task.FromResult<string?>(null);
        _serialPort?.Close();
        return Task.FromResult((string?) _platformIoPort.Port);
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
    
    public bool Is32U4()
    {
        return Board.Is32U4();
    }

    public void Reconnect()
    {
        if (!IsMini() || _platformIoPort is null) return;
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

        Load();
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }


    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return _microcontroller;
    }

    public void StartTicking(ConfigViewModel model)
    {
        _model = model;
        _timer.Start();
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
                    var wValue = (ushort) (microcontroller.GetChannel(pin, true) | (mask << 8));
                    var val = BitConverter.ToUInt16(await ReadDataAsync(wValue, (byte) Commands.CommandReadAnalog,
                        sizeof(ushort)));
                    if (analogVals.TryGetValue(pin, out var analogVal))
                    {
                        if (Math.Abs(analogVal - val) <= 1000) continue;
                        _picking = false;
                        return pin;
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
                var wValue = (ushort) (port | (mask << 8));
                var pins = (byte) ((await ReadDataAsync(wValue, (byte) Commands.CommandReadDigital, sizeof(byte)))[0] &
                                   mask);
                if (tickedPorts.ContainsKey(port))
                    if (tickedPorts[port] != pins)
                    {
                        Dictionary<int, bool> outPins = new();
                        // Xor the old and new values to work out what changed, and then return the first changed bit
                        // Note that we also need to invert this, as pinsFromPortMask is configured assuming a pull up is in place,
                        // Which would then be expecting a zero for a active pin and a 1 for a inactive pin.
                        microcontroller.PinsFromPortMask(port, mask, (byte) ~(pins ^ tickedPorts[port]), outPins);
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
        if (InvalidDevice)
        {
            return "Santroller - please disconnect and reconnect in PC mode";
        }

        var ret = $"Santroller - {Board.Name}";
        if (_deviceControllerType != null) ret += $" - {_deviceControllerType}";

        return ret;
    }

    public enum Commands
    {
        CommandReboot = 0x30,
        CommandJumpBootloader,
        CommandJumpBootloaderUno,
        CommandJumpBootloaderUnoUsbThenSerial,
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
        CommandReadSerial,
        CommandReadRf,
        CommandReadUsbHost,
        CommandStartBtScan,
        CommandStopBtScan,
        CommandGetBtDevices,
        CommandGetBtState,
        CommandGetBtAddress
    }


    public string GetSerialPort()
    {
        return _platformIoPort?.Port ?? "";
    }

    public void ClearLed(byte led)
    {
        WriteData(0, (byte) Commands.CommandSetLeds, new byte[] {led, 0, 0, 0});
    }

    public void SetLed(byte led, byte[] color)
    {
        _ledTimers[led] = _sw.Elapsed;
        WriteData(0, (byte) Commands.CommandSetLeds, new[] {led}.Concat(color).ToArray());
    }

    public void StartScan()
    {
        WriteData(0, (byte) Commands.CommandStartBtScan, Array.Empty<byte>());
    }

    public void StopScan()
    {
        WriteData(0, (byte) Commands.CommandStopBtScan, Array.Empty<byte>());
    }

    public byte[] GetBtScanResults()
    {
        return !IsPico() ? Array.Empty<byte>() :ReadData(0, (byte) Commands.CommandGetBtDevices);
    }

    public string GetBluetoothAddress()
    {
        return !IsPico() ? "" : Encoding.Default.GetString(ReadData(0, (byte) Commands.CommandGetBtAddress));
    }

    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
        }
    }
}