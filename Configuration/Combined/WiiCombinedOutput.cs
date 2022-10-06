using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;

public class WiiCombinedOutput : TwiOutput
{
    public static readonly Dictionary<WiiInputType, StandardButtonType> Buttons = new()
    {
        {WiiInputType.ClassicA, StandardButtonType.A},
        {WiiInputType.ClassicB, StandardButtonType.B},
        {WiiInputType.ClassicX, StandardButtonType.X},
        {WiiInputType.ClassicY, StandardButtonType.Y},
        {WiiInputType.ClassicLt, StandardButtonType.LT},
        {WiiInputType.ClassicRt, StandardButtonType.RT},
        {WiiInputType.ClassicZl, StandardButtonType.LB},
        {WiiInputType.ClassicZr, StandardButtonType.RB},
        {WiiInputType.ClassicMinus, StandardButtonType.Select},
        {WiiInputType.ClassicPlus, StandardButtonType.Start},
        {WiiInputType.ClassicDPadDown, StandardButtonType.Down},
        {WiiInputType.ClassicDPadUp, StandardButtonType.Up},
        {WiiInputType.ClassicDPadLeft, StandardButtonType.Left},
        {WiiInputType.ClassicDPadRight, StandardButtonType.Right},
        {WiiInputType.DjHeroLeftGreen, StandardButtonType.A},
        {WiiInputType.DjHeroLeftRed, StandardButtonType.B},
        {WiiInputType.DjHeroLeftBlue, StandardButtonType.X},
        {WiiInputType.DjHeroRightGreen, StandardButtonType.A},
        {WiiInputType.DjHeroRightRed, StandardButtonType.B},
        {WiiInputType.DjHeroRightBlue, StandardButtonType.X},
        {WiiInputType.DjHeroLeftAny, StandardButtonType.LB},
        {WiiInputType.DjHeroRightAny, StandardButtonType.RB},
        {WiiInputType.DjHeroEuphoria, StandardButtonType.Y},
        {WiiInputType.NunchukC, StandardButtonType.A},
        {WiiInputType.NunchukZ, StandardButtonType.B},
        {WiiInputType.GuitarGreen, StandardButtonType.A},
        {WiiInputType.GuitarRed, StandardButtonType.B},
        {WiiInputType.GuitarYellow, StandardButtonType.Y},
        {WiiInputType.GuitarBlue, StandardButtonType.X},
        {WiiInputType.GuitarOrange, StandardButtonType.LB},
        {WiiInputType.GuitarStrumDown, StandardButtonType.Down},
        {WiiInputType.GuitarStrumUp, StandardButtonType.Up},
        {WiiInputType.GuitarMinus, StandardButtonType.Select},
        {WiiInputType.GuitarPlus, StandardButtonType.Start},
        {WiiInputType.UDrawPenClick, StandardButtonType.A},
        {WiiInputType.UDrawPenButton1, StandardButtonType.X},
        {WiiInputType.UDrawPenButton2, StandardButtonType.Y},
        {WiiInputType.TaTaConLeftDrumCenter, StandardButtonType.A},
        {WiiInputType.TaTaConLeftDrumRim, StandardButtonType.B},
        {WiiInputType.TaTaConRightDrumCenter, StandardButtonType.X},
        {WiiInputType.TaTaConRightDrumRim, StandardButtonType.Y},
        {WiiInputType.DrumGreen, StandardButtonType.A},
        {WiiInputType.DrumRed, StandardButtonType.B},
        {WiiInputType.DrumYellow, StandardButtonType.Y},
        {WiiInputType.DrumBlue, StandardButtonType.X},
        {WiiInputType.DrumOrange, StandardButtonType.LB},
        {WiiInputType.DrumKickPedal, StandardButtonType.RB},
        //TODO: not really sure what to map this to yet?
        {WiiInputType.DrumHiHatPedal, StandardButtonType.RB},
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> Axis = new()
    {
        {WiiInputType.ClassicLeftStickX, StandardAxisType.LeftStickX},
        {WiiInputType.ClassicLeftStickY, StandardAxisType.LeftStickY},
        {WiiInputType.ClassicRightStickX, StandardAxisType.RightStickX},
        {WiiInputType.ClassicRightStickY, StandardAxisType.RightStickY},
        {WiiInputType.ClassicLeftTrigger, StandardAxisType.LeftTrigger},
        {WiiInputType.ClassicRightTrigger, StandardAxisType.RightTrigger},
        {WiiInputType.DjCrossfadeSlider, StandardAxisType.RightStickY},
        {WiiInputType.DjEffectDial, StandardAxisType.RightStickX},
        {WiiInputType.DjTurntableLeft, StandardAxisType.LeftStickX},
        {WiiInputType.DjTurntableRight, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenX, StandardAxisType.LeftStickX},
        {WiiInputType.UDrawPenY, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.DrawsomePenX, StandardAxisType.LeftStickX},
        {WiiInputType.DrawsomePenY, StandardAxisType.LeftStickY},
        {WiiInputType.DrawsomePenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.NunchukStickX, StandardAxisType.LeftStickX},
        {WiiInputType.NunchukStickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarJoystickX, StandardAxisType.LeftStickX},
        {WiiInputType.GuitarJoystickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarWhammy, StandardAxisType.RightStickX}
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> AxisAcceleration = new()
    {
        {WiiInputType.NunchukRotationRoll, StandardAxisType.RightStickX},
        {WiiInputType.NunchukRotationPitch, StandardAxisType.RightStickY},
    };

    private readonly List<Output> _bindings = new();
    private readonly List<Output> _bindingsAcceleration = new();
    private readonly List<Output> _bindingsDPad = new();
    //TODO: use the tap bar implementation we have in ardwiino, gh5 tap bar works the same way.
    private readonly List<Output> _bindingsFrets = new();

    private readonly Output _bindingTap;

    public bool MapTapBarToFrets { get; set; }
    public bool MapTapBarToAxis { get; set; }
    public bool MapGuitarJoystickToDPad { get; set; }
    public bool MapNunchukAccelerationToRightJoy { get; set; }

    public WiiCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda=null, int? scl=null, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false, bool mapGuitarJoystickToDPad = false, bool mapNunchukAccelerationToRightJoy = false): base(model, microcontroller, WiiInput.WiiTwiType, WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        MapTapBarToFrets = mapTapBarToFrets;
        MapTapBarToAxis = mapTapBarToAxis;
        MapGuitarJoystickToDPad = mapGuitarJoystickToDPad;
        MapNunchukAccelerationToRightJoy = mapNunchukAccelerationToRightJoy;
        foreach (var pair in Buttons)
        {
            _bindings.Add(new ControllerButton(model, new WiiInput(pair.Key, microcontroller), Colors.Transparent, Colors.Transparent, 10,
                pair.Value));
        }
        foreach (var pair in Axis)
        {
            _bindings.Add(new ControllerAxis(model, new WiiInput(pair.Key, microcontroller), Colors.Transparent,
                Colors.Transparent, 1, 0, 0, pair.Value));
        }

        foreach (var pair in AxisAcceleration)
        {
            _bindingsAcceleration.Add(new ControllerAxis(model, new WiiInput(pair.Key, microcontroller), Colors.Transparent,
                Colors.Transparent, 1, 0, 0, pair.Value));
        }
        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));

        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, microcontroller), AnalogToDigitalType.JoyHigh, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Right));

        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Up));

        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Down));
        _bindingsDPad.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.GuitarJoystickX, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));

        _bindingsDPad.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.GuitarJoystickX, microcontroller), AnalogToDigitalType.JoyHigh, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Right));

        _bindingsDPad.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.GuitarJoystickY, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Up));

        _bindingsDPad.Add(new ControllerButton(model,
            new AnalogToDigital(new WiiInput(WiiInputType.GuitarJoystickY, microcontroller), AnalogToDigitalType.JoyLow, 32),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Down));
        _bindingTap = new ControllerAxis(model, new WiiInput(WiiInputType.GuitarTapBar, microcontroller), Colors.Transparent,
            Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY);
    }

    public override bool IsCombined => true;

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override SerializedOutput GetJson()
    {
        return new SerializedWiiCombinedOutput(LedOn, LedOff, Sda, Scl, MapTapBarToFrets, MapTapBarToAxis, MapGuitarJoystickToDPad, MapNunchukAccelerationToRightJoy);
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();
    
    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new(_bindings);

        if (MapTapBarToAxis)
        {
            outputs.Add(_bindingTap);
        }

        if (MapGuitarJoystickToDPad)
        {
            outputs.AddRange(_bindingsDPad);
        }

        if (MapNunchukAccelerationToRightJoy)
        {
            outputs.AddRange(_bindingsAcceleration);
        }

        if (MapTapBarToFrets)
        {
            outputs.AddRange(_bindingsFrets);
        }

        return outputs.AsReadOnly();
    }
}