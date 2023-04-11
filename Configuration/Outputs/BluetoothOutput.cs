using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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

public class BluetoothOutput : CombinedOutput
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
            return new List<string>
            {
                "BLUETOOTH_RX",
            };
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
        _macAddress = macAddress;
    }

    private string _macAddress;

    public string MacAddress
    {
        get => _macAddress;
        set => this.RaiseAndSetIfChanged(ref _macAddress, value);
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
        
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return "";
    }


    public override void UpdateBindings()
    {
    }
}