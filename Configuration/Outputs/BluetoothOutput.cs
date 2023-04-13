using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class BluetoothOutput : CombinedOutput
{
    private class BluetoothInput : Input
    {
        public BluetoothInput(BluetoothOutput bluetoothOutput) : base(bluetoothOutput.Model)
        {
            BluetoothOutput = bluetoothOutput;
        }

        public BluetoothOutput BluetoothOutput { get; }

        public override bool IsUint => false;
        public override IList<DevicePin> Pins => new List<DevicePin>();
        public override IList<PinConfig> PinConfigs => new List<PinConfig>();
        public override InputType? InputType => Types.InputType.RfInput;

        public override void Dispose()
        {
        }

        public override string Title => "Bluetooth";

        public override IReadOnlyList<string> RequiredDefines()
        {
            var ret = new List<string>
            {
                "BLUETOOTH_RX",
            };
            if (BluetoothOutput.MacAddress != NoDeviceText)
            {
                ret.Add("BT_ADDR="+BluetoothOutput.MacAddress);
            }

            return ret;
        }

        public override string Generate(ConfigField mode)
        {
            return "";
        }

        public override SerializedInput Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
            Dictionary<int, bool> digitalRaw, byte[] ps2Raw, byte[] wiiRaw,
            byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
            byte[] wiiControllerType)
        {
        }

        public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings,
            ConfigField mode)
        {
            return "";
        }

        public override string GetImagePath()
        {
            return BluetoothOutput.GetImagePath(Model.DeviceType, Model.RhythmType);
        }
    }


    public BluetoothOutput(ConfigViewModel model, string macAddress) : base(model, new FixedInput(model, 0))
    {
        Input = new BluetoothInput(this);
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Tick;
        _scanText = this.WhenAnyValue(s => s.ScanTimer).Select(scanTimer => scanTimer == 11 ? "Start Scan" : $"Scanning... ({scanTimer})").ToProperty(this, x => x.ScanText);
        _scanning = this.WhenAnyValue(s => s.ScanTimer).Select(scanTimer => scanTimer != 11).ToProperty(this, x => x.Scanning);
        Addresses.Add(macAddress.Any() ? macAddress : NoDeviceText);
        _macAddress = Addresses.First();
        if (Model.Device is Santroller santroller)
        {
            LocalAddress = santroller.GetBluetoothAddress();
        }
        else
        {
            LocalAddress = "Write config to retrieve address";
        }
    }


    private const int BtAddressLength = 18;
    private const string NoDeviceText = "No device found";
    private readonly ObservableAsPropertyHelper<string> _scanText;
    private readonly ObservableAsPropertyHelper<bool> _scanning;
    public string LocalAddress { get; }
    private string _macAddress;
    private bool _connected = false;

    public AvaloniaList<string> Addresses { get; } = new();

    private DispatcherTimer _timer = new();

    private int _scanTimer = 11;

    public string ScanText => _scanText.Value;

    public bool Scanning => _scanning.Value;

    public int ScanTimer
    {
        get => _scanTimer;
        set => this.RaiseAndSetIfChanged(ref _scanTimer, value);
    }

    public string MacAddress
    {
        get => _macAddress;
        set => this.RaiseAndSetIfChanged(ref _macAddress, value);
    }

    public bool Connected
    {
        get => _connected;
        set => this.RaiseAndSetIfChanged(ref _connected, value);
    }

    public override bool IsCombined => true;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;

    public override bool Valid => true;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override SerializedOutput Serialize()
    {
        return new SerializedBluetoothOutput(MacAddress);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "bluetooth.png";
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Bluetooth Input";
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw, byte[] wiiRaw,
        byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType, byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw);
        if (!bluetoothRaw.Any()) return;
        Connected = bluetoothRaw[0] != 0;
    }

    
    [RelayCommand]
    public void Scan()
    {
        if (Model.Device is not Santroller santroller)
        {
            return;
        }

        _timer.Start();
        ScanTimer--;
        santroller.StartScan();
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (Model.Device is not Santroller santroller)
        {
            return;
        }

        ScanTimer--;

        var addresses = santroller.GetBtScanResults();
        var deviceCount = addresses.Length / BtAddressLength;
        var addressesAsStrings = new List<string>();
        for (var i = 0; i < deviceCount; i++)
        {
            addressesAsStrings.Add(
                Encoding.Default.GetString(addresses[(i * BtAddressLength)..((i + 1) * BtAddressLength)]));
        }

        if (deviceCount != 0)
        {
            var setNew = addressesAsStrings.ToHashSet();
            var setOld = Addresses.ToHashSet();
            var wasUnset = MacAddress == NoDeviceText;
            if (!wasUnset)
            {
                setNew.Add(MacAddress); 
            }
            Addresses.Remove(setOld.Except(setNew));
            Addresses.Add(setNew.Except(setOld));
            if (wasUnset)
            {
                MacAddress = Addresses.First();
            }

        } else if (MacAddress == NoDeviceText)
        {
            Addresses.Clear();
            Addresses.Add(NoDeviceText);
        }

        if (ScanTimer != 0) return;
        ScanTimer = 11;
        _timer.Stop();
    }


    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return "";
    }


    public override void UpdateBindings()
    {
    }
}