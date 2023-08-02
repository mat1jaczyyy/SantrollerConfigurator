using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Devices;

public class Santroller : ConfigurableUsbDevice
{
    public static readonly Guid DeviceGuid = Guid.Parse("{DF59037D-7C92-4155-AC12-7D700A313D79}");
    private const int BtAddressLength = 18;

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
        CommandGetBtAddress,
        CommandReadUsbHostInputs
    }

    private readonly Dictionary<int, int> _analogRaw = new();
    private readonly Dictionary<int, bool> _digitalRaw = new();
    private readonly Dictionary<byte, TimeSpan> _ledTimers = new();
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private DeviceControllerType _deviceControllerType;
    private Microcontroller _microcontroller;
    private ConfigViewModel? _model;
    private byte[]? _lastConfig;
    private byte[]? _currentConfigData;
    private SerializedConfiguration? _currentConfig;
    private bool _picking;
    private readonly DispatcherTimer _timer;
    private ReadOnlyObservableCollection<Output>? _bindings;
    private readonly ConsoleType _currentMode;

    public Santroller(string path, UsbDevice device, string serial,
        ushort version) : base(
        device, path, serial, version)
    {
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, Tick);
        _microcontroller = new Pico(Board.Generic);
        _deviceControllerType = (DeviceControllerType) (version >> 8);
        _currentMode = (ConsoleType) (serial[^3] - '0');
        if (device is IUsbDevice usbDevice) usbDevice.ClaimInterface(2);
#if Windows
        var isXinput = (serial[^1] - '0') != 0;
        if (isXinput && _currentMode != ConsoleType.Windows)
        {
            InvalidDevice = true;
            return;
        }
#endif
        if (_currentMode is not (ConsoleType.Universal or ConsoleType.Windows))
        {
            InvalidDevice = true;
            return;
        }

        Load();
        if (Board.Name == Board.Generic.Name)
        {
            InvalidDevice = true;
        }
    }

    private string GetString(byte[] str)
    {
        return Encoding.UTF8.GetString(str).Replace("\0", "");
    }

    private bool InvalidDevice { get; }

    public override bool MigrationSupported => true;

    public override void Bootloader()
    {
        if (Board.HasUsbmcu)
            WriteData(0, (byte) Commands.CommandJumpBootloaderUnoUsbThenSerial, Array.Empty<byte>());
        else
            WriteData(0, (byte) Commands.CommandJumpBootloader, Array.Empty<byte>());

        Device.Close();
    }

    public override void BootloaderUsb()
    {
        WriteData(0, (byte) Commands.CommandJumpBootloaderUno, Array.Empty<byte>());
        Device.Close();
    }

    public override bool LoadConfiguration(ConfigViewModel model)
    {
        _ = LoadConfigurationAsync(model);
        return true;
    }


    public override void Revert()
    {
        Bootloader();
    }


    public override Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        return _microcontroller;
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_model == null || _bindings == null) return;
        Diff();
        if (!Device.IsOpen || _model.Main.Working)
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
            var direct = _model.Bindings.Items.Select(s => s.Input.InnermostInput())
                .OfType<DirectInput>().ToList();
            var digital = direct.Where(s => !s.IsAnalog).SelectMany(s => s.Pins).Distinct().Where(s => s.Pin != -1);
            var analog = direct.Where(s => s.IsAnalog).SelectMany(s => s.Pins).Distinct().Where(s => s.Pin != -1);
            var ports = _model.Microcontroller.GetPortsForTicking(digital);

            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var data = ReadData(wValue, (byte) Commands.CommandReadDigital, sizeof(byte));
                if (data.Length == 0) return;

                var pins = data[0];
                _model.Microcontroller.PinsFromPortMask(port, mask, pins, _digitalRaw);
            }

            foreach (var devicePin in analog)
            {
                var mask = _model.Microcontroller.GetAnalogMask(devicePin);
                var wValue = (ushort) (_model.Microcontroller.GetChannel(devicePin.Pin, false) | (mask << 8));
                var val = BitConverter.ToUInt16(ReadData(wValue, (byte) Commands.CommandReadAnalog,
                    sizeof(ushort)));
                _analogRaw[devicePin.Pin] = val;
            }

            var ps2Raw = ReadData(0, (byte) Commands.CommandReadPs2, 9);
            var wiiRaw = ReadData(0, (byte) Commands.CommandReadWii, 8);
            var djLeftRaw = ReadData(0, (byte) Commands.CommandReadDjLeft, 3);
            var djRightRaw = ReadData(0, (byte) Commands.CommandReadDjRight, 3);
            var gh5Raw = ReadData(0, (byte) Commands.CommandReadGh5, 2);
            var ghWtRaw = ReadData(0, (byte) Commands.CommandReadGhWt, sizeof(int));
            var ps2ControllerType = ReadData(0, (byte) Commands.CommandGetExtensionPs2, 1);
            var wiiControllerType = ReadData(0, (byte) Commands.CommandGetExtensionWii, sizeof(short));
            var usbHostRaw = Array.Empty<byte>();
            var usbHostInputsRaw = Array.Empty<byte>();
            if (_model.UsbHostEnabled)
            {
                usbHostRaw = ReadData(0, (byte) Commands.CommandReadUsbHost, 24);
                usbHostInputsRaw = ReadData(0, (byte) Commands.CommandReadUsbHostInputs, 100);
            }

            var bluetoothRaw = Array.Empty<byte>();
            if (IsPico()) bluetoothRaw = ReadData(0, (byte) Commands.CommandGetBtState, 1);


            _model.Update(bluetoothRaw);
            foreach (var output in _bindings)
                output.Update(_analogRaw, _digitalRaw, ps2Raw, wiiRaw, djLeftRaw,
                    djRightRaw, gh5Raw,
                    ghWtRaw, ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void Load()
    {
        var fCpuStr = GetString(ReadData(0, (byte) Commands.CommandReadFCpu, 32)).Replace("L", "").Trim();
        if (!fCpuStr.Any()) return;

        var fCpu = uint.Parse(fCpuStr);
        var board = GetString(ReadData(0, (byte) Commands.CommandReadBoard, 32));
        var m = Board.FindMicrocontroller(Board.FindBoard(board, fCpu));
        Board = m.Board;
        _microcontroller = m;
    }


    private async Task LoadConfigurationAsync(ConfigViewModel model)
    {
        ushort start = 0;
        var data = new List<byte>();
        while (true)
        {
            var chunk = ReadData(start, (byte) Commands.CommandReadConfig, 64);
            if (!chunk.Any()) break;
            data.AddRange(chunk);
            start += 64;
        }

        using var inputStream = new MemoryStream(data.ToArray());
        await using var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress);
        try
        {
            var lastConfig = Serializer.Deserialize<SerializedConfiguration>(decompressor);
            lastConfig.LoadConfiguration(model);
            lastConfig = new SerializedConfiguration(model);
            using var outputStream = new MemoryStream();
            Serializer.Serialize(outputStream, lastConfig);
            _lastConfig = outputStream.ToArray();
            _currentConfig = new SerializedConfiguration(model);
            _currentConfigData = new byte[_lastConfig.Length];
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.StackTrace);
        }

        _deviceControllerType = model.DeviceControllerType;

        _model = model;
        model.Bindings.Connect().Bind(out var bindings).Subscribe();
        _bindings = bindings;
        _timer.Start();
    }

    private void Diff()
    {
        if (_model == null || _currentConfig == null || _lastConfig == null || _currentConfigData == null ||
            _bindings == null) return;
        _currentConfig.Update(_model, _bindings, false);
        using var outputStream = new MemoryStream(_currentConfigData);
        try
        {
            Serializer.Serialize(outputStream, _currentConfig);
            _model.Main.SetDifference(!_currentConfigData.AsSpan().SequenceEqual(_lastConfig.AsSpan()));
        }
        catch (Exception)
        {
            _model.Main.SetDifference(true);
        }
    }

    public void StartTicking(ConfigViewModel model)
    {
        _model = model;

        model.Bindings.Connect().Bind(out var allOutputs).Subscribe();
        _bindings = allOutputs;
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
        foreach (var config in _model!.GetPinConfigs())
            switch (config)
            {
                case SpiConfig spi:
                    importantPins.AddRange(spi.Pins.Where(s => s != -1));
                    break;
                case TwiConfig twi:
                    importantPins.AddRange(twi.Pins.Where(s => s != -1));
                    break;
                case DirectPinConfig direct:
                    if (!direct.Type.Contains("-")) importantPins.AddRange(direct.Pins.Where(s => s != -1));
                    break;
            }

        if (analog)
        {
            var pins = microcontroller.AnalogPins.Except(importantPins).ToList();
            var analogVals = new Dictionary<int, int>();
            foreach (var pin in pins)
            {
                var devicePin = new DevicePin(pin, DevicePinMode.PullUp);
                var mask = microcontroller.GetAnalogMask(devicePin);
                var wValue = (ushort) (microcontroller.GetChannel(pin, true) | (mask << 8));
                ReadData(wValue, (byte) Commands.CommandReadAnalog, sizeof(ushort));
            }

            while (_picking)
            {
                foreach (var pin in pins)
                {
                    var devicePin = new DevicePin(pin, DevicePinMode.PullUp);
                    var mask = microcontroller.GetAnalogMask(devicePin);
                    var wValue = (ushort) (microcontroller.GetChannel(pin, true) | (mask << 8));
                    var val = BitConverter.ToUInt16(ReadData(wValue, (byte) Commands.CommandReadAnalog,
                        sizeof(ushort)));
                    if (analogVals.TryGetValue(pin, out var analogVal))
                    {
                        if (Math.Abs(analogVal - val) <= 3000) continue;
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
        foreach (var (port, mask) in ports)
        {
            var wValue = (ushort) (port | (mask << 8));
            ReadData(wValue, (byte) Commands.CommandReadDigital, sizeof(byte));
        }

        Dictionary<int, byte> tickedPorts = new();
        while (_picking)
        {
            foreach (var (port, mask) in ports)
            {
                var wValue = (ushort) (port | (mask << 8));
                var pins = (byte) ((ReadData(wValue, (byte) Commands.CommandReadDigital, sizeof(byte)))[0] &
                                   mask);
                if (tickedPorts.ContainsKey(port))
                {
                    if (tickedPorts[port] != pins)
                    {
                        Dictionary<int, bool> outPins = new();
                        // Xor the old and new values to work out what changed, and then return the first changed bit
                        // Note that we also need to invert this, as pinsFromPortMask is configured assuming a pull up is in place,
                        // Which would then be expecting a zero for a active pin and a 1 for a inactive pin.
                        microcontroller.PinsFromPortMask(port, mask, (byte) ~(pins ^ tickedPorts[port]), outPins);
                        _picking = false;
                        return outPins.First(s => !s.Value).Key;
                    }
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
            var ret2 = Resources.ErrorNotPCMode;
            if (_currentMode != null)
            {
                ret2 += $" (Currently {EnumToStringConverter.Convert(_currentMode)})";
            }

            return ret2;
        }

        var ret = $"Santroller - {Board.Name}";
        ret += $" - {EnumToStringConverter.Convert(_deviceControllerType)}";

        return ret;
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

    public IReadOnlyList<string> GetBtScanResults()
    {
        if (!IsPico()) return Array.Empty<string>();
        var data = ReadData(0, (byte) Commands.CommandGetBtDevices);
        var addressesAsStrings = new List<string>();
        for (var i = 0; i < data.Length; i += BtAddressLength)
        {
            addressesAsStrings.Add(GetString(data[i..(i + BtAddressLength)]));
        }

        return addressesAsStrings;
    }

    public string GetBluetoothAddress()
    {
        return !IsPico() ? "" : GetString(ReadData(0, (byte) Commands.CommandGetBtAddress));
    }

    public override void Disconnect()
    {
        _timer.Stop();

        base.Disconnect();
    }

    public void StopTicking()
    {
        _timer.Stop();
    }
}