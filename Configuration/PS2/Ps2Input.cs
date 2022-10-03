using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarConfiguratorSharp.NetCore.Configuration.PS2;

public class Ps2Input : Input
{
    public Ps2InputType Input { get; }

    private static readonly List<Ps2InputType> Dualshock2Order = new()
    {
        Ps2InputType.RightX,
        Ps2InputType.RightY,
        Ps2InputType.LeftX,
        Ps2InputType.LeftY,
        Ps2InputType.Dualshock2RightButton,
        Ps2InputType.Dualshock2LeftButton,
        Ps2InputType.Dualshock2UpButton,
        Ps2InputType.Dualshock2DownButton,
        Ps2InputType.Dualshock2Triangle,
        Ps2InputType.Dualshock2Circle,
        Ps2InputType.Dualshock2Cross,
        Ps2InputType.Dualshock2Square,
        Ps2InputType.Dualshock2L1,
        Ps2InputType.Dualshock2R1,
        Ps2InputType.Dualshock2L2,
        Ps2InputType.Dualshock2R2,
    };

    public static readonly List<Ps2InputType> DigitalButtons = new()
    {
        Ps2InputType.L3, 
        Ps2InputType.R3, 
        Ps2InputType.Start,
        Ps2InputType.Select,
        Ps2InputType.Up, 
        Ps2InputType.Right,
        Ps2InputType.Down,
        Ps2InputType.Left,
        Ps2InputType.L2, 
        Ps2InputType.R2, 
        Ps2InputType.L1, 
        Ps2InputType.R1, 
        Ps2InputType.Triangle,
        Ps2InputType.Circle,
        Ps2InputType.Cross,
        Ps2InputType.Square
    };
    private static readonly Dictionary<Ps2InputType, String> Mappings = new()
    {
        {Ps2InputType.LeftX, "(in[7] - 128) << 8"},
        {Ps2InputType.LeftY, "-(in[8] - 127) << 8"},
        {Ps2InputType.MouseX, "(in[5] - 128) << 8"},
        {Ps2InputType.MouseY, "-(in[6] - 127) << 8"},
        {Ps2InputType.RightX, "(in[5] - 128) << 8"},
        {Ps2InputType.RightY, "-(in[6] - 127) << 8"},
        {Ps2InputType.NegConTwist, "(in[5] - 128) << 8"},
        {Ps2InputType.NegConI, "in[6]"},
        {Ps2InputType.NegConIi, "in[7]"},
        {Ps2InputType.NegConL, "in[8]"},
        {Ps2InputType.NegConR, $"in[4] & ({1 << 3})"},
        {Ps2InputType.NegConA, $"in[4] & ({1 << 5})"},
        {Ps2InputType.NegConB, $"in[4] & ({1 << 4})"},
        {Ps2InputType.GunconHSync, "(in[6] << 8) | in[5]"},
        {Ps2InputType.GunconVSync, "(in[8] << 8) | in[7]"},
        {Ps2InputType.JogConWheel, "(in[6] << 8) | in[5]"},
        {Ps2InputType.GuitarWhammy, "-(in[8] - 127) << 8"},
        {Ps2InputType.Dualshock2RightButton, "in[generated]"},
        {Ps2InputType.Dualshock2LeftButton, "in[generated]"},
        {Ps2InputType.Dualshock2UpButton, "in[generated]"},
        {Ps2InputType.Dualshock2DownButton, "in[generated]"},
        {Ps2InputType.Dualshock2Triangle, "in[generated]"},
        {Ps2InputType.Dualshock2Circle, "in[generated]"},
        {Ps2InputType.Dualshock2Cross, "in[generated]"},
        {Ps2InputType.Dualshock2Square, "in[generated]"},
        {Ps2InputType.Dualshock2L1, "in[generated]"},
        {Ps2InputType.Dualshock2R1, "in[generated]"},
        {Ps2InputType.Dualshock2L2, "in[generated]"},
        {Ps2InputType.Dualshock2R2, "in[generated]"},
        {Ps2InputType.GuitarGreen, $"in[4] & ({1 << 1})"},
        {Ps2InputType.GuitarRed, $"in[4] & ({1 << 5})"},
        {Ps2InputType.GuitarYellow, $"in[4] & ({1 << 4})"},
        {Ps2InputType.GuitarBlue, $"in[4] & ({1 << 6})"},
        {Ps2InputType.GuitarOrange, $"in[4] & ({1 << 7})"},
        {Ps2InputType.GuitarSelect, $"in[3] & ({1 << 0})"},
        {Ps2InputType.GuitarStart, $"in[3] & ({1 << 3})"},
        {Ps2InputType.NegConStart, $"in[3] & ({1 << 3})"},
        {Ps2InputType.L3, $"in[3] & ({1 << 1})"},
        {Ps2InputType.R3, $"in[3] & ({1 << 2})"},
        {Ps2InputType.Start, $"in[3] & ({1 << 3})"},
        {Ps2InputType.GuitarStrumUp, $"in[3] & ({1 << 4})"},
        {Ps2InputType.GuitarStrumDown, $"in[3] & ({1 << 6})"},
        {Ps2InputType.Select, $"in[3] & ({1 << 0})"},
        {Ps2InputType.Up, $"in[3] & ({1 << 4})"},
        {Ps2InputType.Right, $"in[3] & ({1 << 5})"},
        {Ps2InputType.Down, $"in[3] & ({1 << 6})"},
        {Ps2InputType.Left, $"in[3] & ({1 << 7})"},
        {Ps2InputType.L2, $"in[4] & ({1 << 0})"},
        {Ps2InputType.R2, $"in[4] & ({1 << 1})"},
        {Ps2InputType.L1, $"in[4] & ({1 << 2})"},
        {Ps2InputType.R1, $"in[4] & ({1 << 3})"},
        {Ps2InputType.Triangle, $"in[4] & ({1 << 4})"},
        {Ps2InputType.Circle, $"in[4] & ({1 << 5})"},
        {Ps2InputType.Cross, $"in[4] & ({1 << 6})"},
        {Ps2InputType.Square, $"in[4] & ({1 << 7})"}
    };

    private static readonly Dictionary<Ps2InputType, Ps2ControllerType> AxisToType =
        new()
        {
            {Ps2InputType.GunconHSync, Ps2ControllerType.JogCon},
            {Ps2InputType.GunconVSync, Ps2ControllerType.JogCon},
            {Ps2InputType.MouseX, Ps2ControllerType.Mouse},
            {Ps2InputType.MouseY, Ps2ControllerType.Mouse},
            {Ps2InputType.NegConTwist, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConI, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConIi, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConL, Ps2ControllerType.NegCon},
            {Ps2InputType.JogConWheel, Ps2ControllerType.JogCon},
            {Ps2InputType.GuitarWhammy, Ps2ControllerType.Guitar},
        };

    private static readonly IReadOnlyList<Ps2InputType> Dualshock = new[]
    {
        Ps2InputType.LeftX,
        Ps2InputType.LeftY,
        Ps2InputType.RightX,
        Ps2InputType.RightY,
    };
    
    private static readonly Dictionary<Ps2ControllerType, string> CType = new() {
        {Ps2ControllerType.Digital, "PSPROTO_DIGITAL"},
        {Ps2ControllerType.Dualshock, "PSPROTO_DUALSHOCK"},
        {Ps2ControllerType.Dualshock2, "PSPROTO_DUALSHOCK2"},
        {Ps2ControllerType.FlightStick, "PSPROTO_FLIGHTSTICK"},
        {Ps2ControllerType.NegCon, "PSPROTO_NEGCON"},
        {Ps2ControllerType.JogCon, "PSPROTO_JOGCON"},
        {Ps2ControllerType.GunCon, "PSPROTO_GUNCON"},
        {Ps2ControllerType.Guitar, "PSPROTO_GUITAR"},
        {Ps2ControllerType.Mouse, "PSPROTO_MOUSE"},
    };

    public Ps2Input(Ps2InputType input)
    {
        Input = input;
    }

    public override string Generate()
    {
        return Mappings[Input];
    }

    public override bool IsAnalog => Input <= Ps2InputType.Dualshock2R2;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        Dictionary<Ps2InputType, string> ds2Axis = new();
        Dictionary<Ps2ControllerType, List<string>> mappedBindings = new();
        foreach (var binding in bindings)
        {
            if (binding.Item1 is Ps2Input input)
            {
                List<Ps2ControllerType> types = new List<Ps2ControllerType>();
                if (AxisToType.ContainsKey(input.Input))
                {
                    types.Add(AxisToType[input.Input]);
                }
                else if (Dualshock2Order.Contains(input.Input))
                {
                    ds2Axis[input.Input] = binding.Item2;
                } else if (DigitalButtons.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Digital);
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.Dualshock2);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                if (Dualshock.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                foreach (var type in types)
                {
                    if (!mappedBindings.ContainsKey(type))
                    {
                        mappedBindings.Add(type, new List<string>());
                    }

                    mappedBindings[type].Add(binding.Item2);
                }
            }
        }

        int i = 0;
        string retDs2 = "";
        foreach (var binding in Dualshock2Order)
        {
            if (ds2Axis.ContainsKey(binding))
            {
                retDs2 += ds2Axis[binding].Replace("generated", i.ToString()) +";\n";
                i++;
            }
        }

        //TODO: we also need to generate the ps2 init somehow too for this, otherwise it wont work!
        if (!string.IsNullOrEmpty(retDs2))
        {
            var mappings = mappedBindings.GetValueOrDefault(Ps2ControllerType.Dualshock2, new List<string>());
            mappings.Add(retDs2);
            mappedBindings[Ps2ControllerType.Dualshock2] = mappings;
        }

        var ret = "";
        foreach (var binding in mappedBindings)
        {
            var input = binding.Key;
            var mappings = binding.Value;
            ret += @$"case {CType[input]}:
    {String.Join(";\n", mappings)};
    break;";
        }

        return ret; 
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_PS2"};
    }
}