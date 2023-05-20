using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class UsbHostCombinedOutput : CombinedOutput
{
    public UsbHostCombinedOutput(ConfigViewModel model) : base(
        model)
    {
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
        Outputs.Connect().Filter(x => x is OutputAxis)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Bind(out var digitalOutputs)
            .Subscribe();
        UpdateDetails();
    }

    [Reactive] public string UsbHostInfo { get; set; } = "";

    [Reactive] public int ConnectedDevices { get; set; }
    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";
    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => Model.AvailablePins.Skip(1).ToList();
    public List<int> AvailablePinsDp => Model.AvailablePins.SkipLast(1).ToList();

    public override SerializedOutput Serialize()
    {
        return new SerializedCombinedUsbHostOutput();
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Usb Host Inputs";
    }

    public override object GetOutputType()
    {
        return SimpleType.UsbHost;
    }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        //TODO this   
    }

    public override void UpdateBindings()
    {
        //TODO this
    }

    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;
    public int UsbHostDm
    {
        get => _usbHostDm.Value;
        set => Model.UsbHostDm = value;
    }
    
    public int UsbHostDp
    {
        get => _usbHostDp.Value;
        set => Model.UsbHostDp = value;
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw, byte[] wiiRaw,
        byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType, byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw, byte[] usbHostInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
        var buffer = "";
        if (!usbHostRaw.Any()) return;
        for (var i = 0; i < usbHostRaw.Length; i += 3)
        {
            var consoleType = (ConsoleType) usbHostRaw[i];
            string subType;
            var rhythmType = "";
            if (consoleType == ConsoleType.Xbox360)
            {
                var xInputSubType = (XInputSubType) usbHostRaw[i + 1];
                subType = EnumToStringConverter.Convert(xInputSubType);
                if (xInputSubType is XInputSubType.Drums or XInputSubType.Guitar or XInputSubType.GuitarAlternate)
                    rhythmType = " " + EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
            }
            else
            {
                var deviceType = (DeviceControllerType) usbHostRaw[i + 1];
                subType = EnumToStringConverter.Convert(deviceType);
                if (deviceType is DeviceControllerType.Drum or DeviceControllerType.Guitar)
                    rhythmType = " " + EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
            }

            buffer += $"{consoleType} {rhythmType} {subType}\n";
        }

        ConnectedDevices = usbHostRaw.Length / 3;

        UsbHostInfo = buffer.Trim();
        UpdateDetails();
    }
}