using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class Ps2CombinedOutput : SpiOutput
{
    public static readonly Dictionary<Ps2InputType, StandardButtonType> Buttons = new()
    {
        {Ps2InputType.Cross, StandardButtonType.A},
        {Ps2InputType.Circle, StandardButtonType.B},
        {Ps2InputType.Square, StandardButtonType.X},
        {Ps2InputType.Triangle, StandardButtonType.Y},
        {Ps2InputType.L1, StandardButtonType.LT},
        {Ps2InputType.R1, StandardButtonType.RT},
        {Ps2InputType.L2, StandardButtonType.LB},
        {Ps2InputType.R2, StandardButtonType.RB},
        {Ps2InputType.L3, StandardButtonType.LeftStick},
        {Ps2InputType.R3, StandardButtonType.RightStick},
        {Ps2InputType.Select, StandardButtonType.Select},
        {Ps2InputType.Start, StandardButtonType.Start},
        {Ps2InputType.Down, StandardButtonType.Down},
        {Ps2InputType.Up, StandardButtonType.Up},
        {Ps2InputType.Left, StandardButtonType.Left},
        {Ps2InputType.Right, StandardButtonType.Right},
        {Ps2InputType.GuitarGreen, StandardButtonType.A},
        {Ps2InputType.GuitarRed, StandardButtonType.B},
        {Ps2InputType.GuitarYellow, StandardButtonType.Y},
        {Ps2InputType.GuitarBlue, StandardButtonType.X},
        {Ps2InputType.GuitarOrange, StandardButtonType.LB},
        {Ps2InputType.GuitarStrumDown, StandardButtonType.Down},
        {Ps2InputType.GuitarStrumUp, StandardButtonType.Up},
        {Ps2InputType.GuitarSelect, StandardButtonType.Select},
        {Ps2InputType.GuitarStart, StandardButtonType.Start},
        {Ps2InputType.NegConR, StandardButtonType.RB},
        {Ps2InputType.NegConA, StandardButtonType.B},
        {Ps2InputType.NegConB, StandardButtonType.Y},
    };

    public static readonly Dictionary<Ps2InputType, StandardAxisType> Axis = new()
    {
        {Ps2InputType.LeftX, StandardAxisType.LeftStickX},
        {Ps2InputType.LeftY, StandardAxisType.LeftStickY},
        {Ps2InputType.RightX, StandardAxisType.RightStickX},
        {Ps2InputType.RightY, StandardAxisType.RightStickY},
        {Ps2InputType.Dualshock2L2, StandardAxisType.LeftTrigger},
        {Ps2InputType.Dualshock2R2, StandardAxisType.RightTrigger},
        {Ps2InputType.GuitarWhammy, StandardAxisType.RightStickX},
        {Ps2InputType.NegConTwist, StandardAxisType.LeftStickX},
        {Ps2InputType.JogConWheel, StandardAxisType.LeftStickX},
        {Ps2InputType.MouseX, StandardAxisType.LeftStickX},
        {Ps2InputType.MouseY, StandardAxisType.LeftStickY},
        {Ps2InputType.GunconHSync, StandardAxisType.LeftStickX},
        {Ps2InputType.GunconVSync, StandardAxisType.LeftStickY},
        {Ps2InputType.NegConL, StandardAxisType.LeftTrigger},
    };

    private readonly List<Output> _bindings = new();

    public Ps2CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? miso = null, int? mosi = null, int? sck = null) : base(model, microcontroller,Ps2Input.Ps2SpiType,Ps2Input.Ps2SpiFreq,Ps2Input.Ps2SpiCpol,Ps2Input.Ps2SpiCpha,Ps2Input.Ps2SpiMsbFirst, "PS2", miso, mosi, sck)
    {
        foreach (var pair in Buttons)
        {
            _bindings.Add(new ControllerButton(model, new Ps2Input(pair.Key, microcontroller), Colors.Transparent, Colors.Transparent,
                10,
                pair.Value));
        }

        foreach (var pair in Axis)
        {
            _bindings.Add(new ControllerAxis(model, new Ps2Input(pair.Key, microcontroller), Colors.Transparent,
                Colors.Transparent, 1, 0, 0, pair.Value));
        }

        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConI, microcontroller), AnalogToDigitalType.Trigger, 128),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConIi, microcontroller), AnalogToDigitalType.Trigger, 128),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
        _bindings.Add(new ControllerButton(model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConL, microcontroller), AnalogToDigitalType.Trigger, 240),
            Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
    }

    public override bool IsCombined => true;

    public override JsonOutput GetJson()
    {
        return new JsonPs2CombinedOutput(LedOn, LedOff, Miso, Mosi, Sck);
    }

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        return _bindings.AsReadOnly();
    }
}