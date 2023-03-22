using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class JoystickToDpadInput : FixedInput
{
    public override string Title => "Map joystick to Dpad";

    public JoystickToDpadInput(ConfigViewModel model) : base(model, 0)
    {
    }
}

public class JoystickToDpad : Output
{
    private int _threshold;

    public int Threshold
    {
        get => _threshold;
        set => this.RaiseAndSetIfChanged(ref _threshold, value);
    }

    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;

    private bool Wii { get; }

    public bool Up
    {
        get => _up;
        set => this.RaiseAndSetIfChanged(ref _up, value);
    }

    public bool Down
    {
        get => _down;
        set => this.RaiseAndSetIfChanged(ref _down, value);
    }

    public bool Left
    {
        get => _left;
        set => this.RaiseAndSetIfChanged(ref _left, value);
    }

    public bool Right
    {
        get => _right;
        set => this.RaiseAndSetIfChanged(ref _right, value);
    }

    private static readonly List<WiiInputType> JoystickToDpadXWii = new()
    {
        WiiInputType.ClassicLeftStickX,
        WiiInputType.NunchukStickX,
        WiiInputType.GuitarJoystickX
    };

    private static readonly List<WiiInputType> JoystickToDpadYWii = new()
    {
        WiiInputType.ClassicLeftStickY,
        WiiInputType.NunchukStickY,
        WiiInputType.GuitarJoystickY
    };

    private List<ControllerButton> _outputs = new();

    public JoystickToDpad(ConfigViewModel model, int threshold, bool wii) : base(
        model, new JoystickToDpadInput(model), Colors.Black, Colors.Black, Array.Empty<byte>())
    {
        Threshold = threshold;
        Wii = wii;
        if (wii)
        {
            foreach (var wiiInputType in JoystickToDpadXWii)
            {
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model), AnalogToDigitalType.JoyLow, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadLeft));
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model), AnalogToDigitalType.JoyHigh, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadRight));
            }

            foreach (var wiiInputType in JoystickToDpadYWii)
            {
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model), AnalogToDigitalType.JoyHigh, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadUp));
                _outputs.Add(new ControllerButton(model,
                    new AnalogToDigital(new WiiInput(wiiInputType, model), AnalogToDigitalType.JoyLow, Threshold,
                        model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadDown));
            }

            UpdateDetails();
            return;
        }

        _outputs.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.LeftX, model), AnalogToDigitalType.JoyLow, Threshold,
                model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadLeft));
        _outputs.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.LeftX, model), AnalogToDigitalType.JoyHigh, Threshold,
                model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadRight));
        _outputs.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.LeftY, model), AnalogToDigitalType.JoyHigh, Threshold,
                model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadUp));
        _outputs.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.LeftY, model), AnalogToDigitalType.JoyLow, Threshold,
                model), Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.DpadDown));
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
        return _outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedJoystickToDpad(Threshold, Wii);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return deviceControllerType is DeviceControllerType.Gamepad or DeviceControllerType.ArcadePad
            or DeviceControllerType.ArcadeStick or DeviceControllerType.FlightStick
            ? "Map Left joystick to Dpad"
            : "Map Joystick to Dpad";
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        var buttons = "DPad";
        if (Up) buttons += "Up";
        if (Down) buttons += "Down";
        if (Left) buttons += "Left";
        if (Right) buttons += "Right";
        return Wii ? $"Wii/Classic{buttons}.png" : $"PS2/{buttons}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return mode is ConfigField.Ps3 or ConfigField.Xbox360 or ConfigField.XboxOne
            ? $"MAP_JOYSTICK_DPAD(report, {Threshold});"
            : "";
    }

    public override void UpdateBindings()
    {
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw, byte[] wiiRaw,
        byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
        foreach (var output in _outputs)
        {
            output.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
                ps2ControllerType, wiiControllerType);
        }

        Up = _outputs.Where(s => s.Type is StandardButtonType.DpadUp)
            .Any(x => x.ValueRaw != 0);
        Down = _outputs.Where(s => s.Type is StandardButtonType.DpadDown)
            .Any(x => x.ValueRaw != 0);
        Left = _outputs.Where(s => s.Type is StandardButtonType.DpadLeft)
            .Any(x => x.ValueRaw != 0);
        Right = _outputs.Where(s => s.Type is StandardButtonType.DpadRight)
            .Any(x => x.ValueRaw != 0);
        UpdateDetails();
    }
}