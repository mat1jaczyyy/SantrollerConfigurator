using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class WiiCombinedOutput : CombinedTwiOutput
{
    public static readonly Dictionary<WiiInputType, StandardButtonType> Buttons = new()
    {
        {WiiInputType.ClassicA, StandardButtonType.A},
        {WiiInputType.ClassicB, StandardButtonType.B},
        {WiiInputType.ClassicX, StandardButtonType.X},
        {WiiInputType.ClassicY, StandardButtonType.Y},
        {WiiInputType.ClassicLt, StandardButtonType.Lt},
        {WiiInputType.ClassicRt, StandardButtonType.Rt},
        {WiiInputType.ClassicZl, StandardButtonType.Lb},
        {WiiInputType.ClassicZr, StandardButtonType.Rb},
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
        {WiiInputType.DjHeroLeftAny, StandardButtonType.Lb},
        {WiiInputType.DjHeroRightAny, StandardButtonType.Rb},
        {WiiInputType.DjHeroEuphoria, StandardButtonType.Y},
        {WiiInputType.NunchukC, StandardButtonType.A},
        {WiiInputType.NunchukZ, StandardButtonType.B},
        {WiiInputType.GuitarGreen, StandardButtonType.A},
        {WiiInputType.GuitarRed, StandardButtonType.B},
        {WiiInputType.GuitarYellow, StandardButtonType.Y},
        {WiiInputType.GuitarBlue, StandardButtonType.X},
        {WiiInputType.GuitarOrange, StandardButtonType.Lb},
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
        {WiiInputType.DrumOrange, StandardButtonType.Lb},
        {WiiInputType.DrumKickPedal, StandardButtonType.Rb},
    };

    public static readonly Dictionary<WiiInputType, StandardButtonType> Tap = new()
    {
        {WiiInputType.GuitarTapGreen, StandardButtonType.A},
        {WiiInputType.GuitarTapRed, StandardButtonType.B},
        {WiiInputType.GuitarTapYellow, StandardButtonType.Y},
        {WiiInputType.GuitarTapBlue, StandardButtonType.X},
        {WiiInputType.GuitarTapOrange, StandardButtonType.Lb},
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

    private readonly Microcontroller _microcontroller;

    private readonly AvaloniaList<Output> _outputs = new();

    public WiiCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, microcontroller, WiiInput.WiiTwiType,
        WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        _microcontroller = microcontroller;
        if (outputs != null)
        {
            _outputs = new AvaloniaList<Output>(outputs);
        }
        else
        {
            CreateDefaults();
        }
    }

    public void CreateDefaults()
    {
        _outputs.Clear();
        foreach (var pair in Buttons)
        {
            _outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, _microcontroller, Sda, Scl),
                Colors.Transparent,
                Colors.Transparent, null, 10,
                pair.Value));
        }

        foreach (var pair in Axis)
        {
            _outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, _microcontroller, Sda, Scl),
                Colors.Transparent,
                Colors.Transparent, null, 1.2f, 0, 10, pair.Value));
        }

        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, _microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Left));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, _microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyHigh, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Right));
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, _microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Up));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, _microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Down));
        _outputs.Add(new ControllerAxis(Model,
            new WiiInput(WiiInputType.GuitarTapBar, _microcontroller, Sda, Scl),
            Colors.Transparent,
            Colors.Transparent, null, 1, 0, 0, StandardAxisType.RightStickY));
    }
    
    public void AddTapBarFrets()
    {
        foreach (var pair in Tap)
        {
            _outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, _microcontroller), Colors.Transparent,
                Colors.Transparent, null, 5, pair.Value));
        }
    }

    public void AddNunchukAcceleration()
    {
        foreach (var pair in AxisAcceleration)
        {
            _outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, _microcontroller, Sda, Scl),
                Colors.Transparent,
                Colors.Transparent, null, 1, 0, 0, pair.Value));
        }
    }

    public override SerializedOutput GetJson()
    {
        return new SerializedWiiCombinedOutput(Sda, Scl, _outputs.ToList());
    }

    public override AvaloniaList<Output> Outputs => _outputs;
}