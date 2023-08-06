using System;
using System.Collections.Generic;
using System.IO;
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
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class BluetoothOutput : CombinedOutput
{
    private readonly DispatcherTimer _timer = new();


    public BluetoothOutput(ConfigViewModel model, string macAddress) : base(model)
    {
        Input = new BluetoothInput(this);
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Tick;
        this.WhenAnyValue(s => s.ScanTimer)
            .Select(scanTimer => scanTimer == 11 ? Resources.BluetoothStartScan : string.Format(Resources.BluetoothScanning, scanTimer))
            .ToPropertyEx(this, x => x.ScanText);
        this.WhenAnyValue(s => s.ScanTimer).Select(scanTimer => scanTimer != 11).ToPropertyEx(this, x => x.Scanning);
        Addresses.Add(macAddress.Any() ? macAddress : Resources.BluetoothNoDevice);
        MacAddress = Addresses.First();
        if (Model.Device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        else
            LocalAddress = Resources.BluetoothWriteConfigMessage;
    }

    [Reactive] public string LocalAddress { get; private set; }

    public AvaloniaList<string> Addresses { get; } = new();

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
        public override InputType? InputType => Types.InputType.BluetoothInput;

        public override string Title => Resources.BluetoothTitle;

        public override IReadOnlyList<string> RequiredDefines()
        {
            var ret = new List<string>
            {
                "BLUETOOTH_RX"
            };
            if (!string.IsNullOrEmpty(BluetoothOutput.MacAddress) && BluetoothOutput.MacAddress.Contains(':')) ret.Add($"BT_ADDR \"{BluetoothOutput.MacAddress}\"");

            return ret;
        }

        public override string Generate()
        {
            return "";
        }

        public override SerializedInput Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(Dictionary<int, int> analogRaw,
            Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
            ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
            ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
            ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw)
        {
        }

        public override string GenerateAll(List<Tuple<Input, string>> bindings,
            ConfigField mode)
        {
            return "";
        }
    } // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public string ScanText { get; } = "";

    [ObservableAsProperty] public bool Scanning { get; }
    // ReSharper enable UnassignedGetOnlyAutoProperty

    [Reactive] public int ScanTimer { get; set; } = 11;

    [Reactive] public string MacAddress { get; set; }

    [Reactive] public bool Connected { get; set; }

    public override bool IsCombined => true;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override SerializedOutput Serialize()
    {
        return new SerializedBluetoothOutput(MacAddress);
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.BluetoothCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.Bluetooth;
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw, ReadOnlySpan<byte> wiiRaw,
        ReadOnlySpan<byte> djLeftRaw, ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw,
        ReadOnlySpan<byte> ghWtRaw, ReadOnlySpan<byte> ps2ControllerType,
        ReadOnlySpan<byte> wiiControllerType, ReadOnlySpan<byte> usbHostRaw, ReadOnlySpan<byte> bluetoothRaw,
        ReadOnlySpan<byte> usbHostInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
        if (LocalAddress == Resources.BluetoothWriteConfigMessage && Model.Device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        if (bluetoothRaw.IsEmpty) return;
        Connected = bluetoothRaw[0] != 0;
    }


    [RelayCommand]
    public void Scan()
    {
        if (Model.Device is not Santroller santroller) return;

        _timer.Start();
        ScanTimer--;
        santroller.StartScan();
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (Model.Device is not Santroller santroller) return;

        ScanTimer--;
        var addresses = santroller.GetBtScanResults();

        if (addresses.Count != 0)
        {
            var setNew = addresses.ToHashSet();
            var setOld = Addresses.ToHashSet();
            var wasUnset = !MacAddress.Contains(":");
            if (!wasUnset) setNew.Add(MacAddress);
            Addresses.Remove(setOld.Except(setNew));
            Addresses.Add(setNew.Except(setOld));
            if (wasUnset) MacAddress = Addresses.First();
        }
        else if (string.IsNullOrWhiteSpace(MacAddress) || !MacAddress.Contains(":"))
        {
            Addresses.Clear();
            Addresses.Add(Resources.BluetoothNoDevice);
            MacAddress = Resources.BluetoothNoDevice;
        }

        if (ScanTimer != 0) return;
        ScanTimer = 11;
        _timer.Stop();
    }


    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return "";
    }


    public override void UpdateBindings()
    {
    }
}