using System;
using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

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
        byte[] wiiControllerType, byte[] rfRaw, byte[] usbHostRaw)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw);
        // TODO: use usbHostRaw here
        UpdateDetails();
    }
}