using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
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
        {Ps2InputType.L2, StandardButtonType.Lb},
        {Ps2InputType.R2, StandardButtonType.Rb},
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
        {Ps2InputType.GuitarOrange, StandardButtonType.Lb},
        {Ps2InputType.GuitarStrumDown, StandardButtonType.Down},
        {Ps2InputType.GuitarStrumUp, StandardButtonType.Up},
        {Ps2InputType.GuitarSelect, StandardButtonType.Select},
        {Ps2InputType.GuitarStart, StandardButtonType.Start},
        {Ps2InputType.NegConR, StandardButtonType.Rb},
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


    public int Ack { get; set; }
    public int Att { get; set; }

    private readonly Microcontroller _microcontroller;

    public Ps2CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? miso = null, int? mosi = null,
        int? sck = null, int? att = null, int? ack = null) : base(model, microcontroller, Ps2Input.Ps2SpiType,
        Ps2Input.Ps2SpiFreq, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, "PS2", miso, mosi, sck)
    {
        _microcontroller = microcontroller;
        Att = att ?? 0;
        Ack = ack ?? 0;
    }

    public override bool IsCombined => true;

    public override SerializedOutput GetJson()
    {
        return new SerializedPs2CombinedOutput(LedOn, LedOff, Miso, Mosi, Sck, Att, Ack);
    }

    public override string Generate(bool xbox, int debounceIndex)
    {
        return "";
    }

    public override IReadOnlyList<Output> GetOutputs(IList<Output> bindings) => GetBindings(bindings);

    private IReadOnlyList<Output> GetBindings(IList<Output> bindings)
    {
        List<Output> outputs = new();
        var inputs = bindings.Select(s => s.Input?.InnermostInput()).Where(s => s is Ps2Input).Cast<Ps2Input>()
            .Select(s => s.Input).ToHashSet();
        foreach (var pair in Buttons)
        {
            if (inputs.Contains(pair.Key)) continue;
            outputs.Add(new ControllerButton(Model, new Ps2Input(pair.Key, _microcontroller, Miso, Mosi, Sck, Att, Ack), Colors.Transparent,
                Colors.Transparent,
                10,
                pair.Value));
        }

        foreach (var pair in Axis)
        {
            if (inputs.Contains(pair.Key)) continue;
            outputs.Add(new ControllerAxis(Model, new Ps2Input(pair.Key, _microcontroller, Miso, Mosi, Sck, Att, Ack), Colors.Transparent,
                Colors.Transparent, 1, 0, 0, pair.Value));
        }

        if (!inputs.Contains(Ps2InputType.NegConI))
        {
            outputs.Add(new ControllerButton(Model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.NegConI, _microcontroller, Miso, Mosi, Sck, Att, Ack),
                    AnalogToDigitalType.Trigger, 128),
                Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
        }


        if (!inputs.Contains(Ps2InputType.NegConIi))
        {
            outputs.Add(new ControllerButton(Model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.NegConIi, _microcontroller, Miso, Mosi, Sck, Att, Ack),
                    AnalogToDigitalType.Trigger, 128),
                Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
        }


        if (!inputs.Contains(Ps2InputType.NegConL))
        {

            outputs.Add(new ControllerButton(Model,
                new AnalogToDigital(new Ps2Input(Ps2InputType.NegConL, _microcontroller, Miso, Mosi, Sck, Att, Ack),
                    AnalogToDigitalType.Trigger, 240),
                Colors.Transparent, Colors.Transparent, 10, StandardButtonType.Left));
        }

        return outputs;
    }
}