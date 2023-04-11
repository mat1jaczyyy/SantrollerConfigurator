using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class UsbHostInputInput : FixedInput
{
    public override string Title => "Usb Host Inputs";

    public UsbHostInputInput(ConfigViewModel model) : base(model, 0)
    {
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_USB_HOST"};
    }
}

public class UsbHostInput : Output
{
    public UsbHostInput(ConfigViewModel model) : base(
        model, new UsbHostInputInput(model), Colors.Black, Colors.Black, Array.Empty<byte>())
    {
        UpdateDetails();
    }

    private int _connectedDevices = 0;
    
    private string _usbHostInfo = "";

    public string UsbHostInfo
    {
        get => _usbHostInfo;
        set => this.RaiseAndSetIfChanged(ref _usbHostInfo, value);
    }

    public int ConnectedDevices
    {
        get => _connectedDevices;
        set => this.RaiseAndSetIfChanged(ref _connectedDevices, value);
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;

    public override bool Valid => true;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override IEnumerable<Output> ValidOutputs()
    {
        return Array.Empty<Output>();
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedUsbHost();
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Usb Host Inputs";
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Usb.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return "";
    }

    public override void UpdateBindings()
    {
    }


    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw, byte[] wiiRaw,
        byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType, byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw);
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
                {
                    rhythmType = " "+EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
                }
            }
            else
            {
                var deviceType = (DeviceControllerType) usbHostRaw[i + 1];
                subType = EnumToStringConverter.Convert(deviceType);
                if (deviceType is DeviceControllerType.Drum or DeviceControllerType.Guitar)
                {
                    rhythmType = " "+EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
                }
                
            }
            buffer += $"{consoleType} {rhythmType} {subType}\n";
        }

        ConnectedDevices = usbHostRaw.Length / 3;

        UsbHostInfo = buffer.Trim();
    }
}