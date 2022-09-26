using System;
using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.PS2;

public class Ps2Input : IInput
{
    public Ps2InputType Input { get; }

    private static readonly List<Ps2InputType> Dualshock2Order = new()
    {
        Ps2InputType.Dualshock2RightX,
        Ps2InputType.Dualshock2RightY,
        Ps2InputType.Dualshock2LeftX,
        Ps2InputType.Dualshock2LeftY,
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

    private static readonly Dictionary<Ps2InputType, String> Mappings = new()
    {
        {Ps2InputType.DualshockLeftX, "(in[7] - 128) << 8"},
        {Ps2InputType.DualshockLeftY, "-(in[8] - 127) << 8"},
        {Ps2InputType.DualshockRightX, "(in[5] - 128) << 8"},
        {Ps2InputType.DualshockRightY, "-(in[6] - 127) << 8"},
        {Ps2InputType.NegConTwist, "(in[5] - 128) << 8"},
        {Ps2InputType.NegConTwistI, "in[6]"},
        {Ps2InputType.NegConTwistIi, "in[7]"},
        {Ps2InputType.NegConTwistL, "in[8]"},
        {Ps2InputType.GunconHSync, "(in[6] << 8) | in[5]"},
        {Ps2InputType.GunconVSync, "(in[8] << 8) | in[7]"},
        {Ps2InputType.JogConWheel, "(in[6] << 8) | in[5]"},
        {Ps2InputType.GuitarWhammy, "-(in[8] - 127) << 8"},
        {Ps2InputType.Dualshock2RightX, "in[generated]"},
        {Ps2InputType.Dualshock2RightY, "in[generated]"},
        {Ps2InputType.Dualshock2LeftX, "in[generated]"},
        {Ps2InputType.Dualshock2LeftY, "in[generated]"},
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
        {Ps2InputType.GuitarGreen, "in[4] >> 1"},
        {Ps2InputType.GuitarRed, "in[4] >> 5"},
        {Ps2InputType.GuitarYellow, "in[4] >> 4"},
        {Ps2InputType.GuitarBlue, "in[4] >> 6"},
        {Ps2InputType.GuitarOrange, "in[4] >> 7"},
        {Ps2InputType.Select, "in[3] >> 0"},
        {Ps2InputType.L3, "in[3] >> 1"},
        {Ps2InputType.R3, "in[3] >> 2"},
        {Ps2InputType.Start, "in[3] >> 3"},
        {Ps2InputType.Up, "in[3] >> 4"},
        {Ps2InputType.Right, "in[3] >> 5"},
        {Ps2InputType.Down, "in[3] >> 6"},
        {Ps2InputType.Left, "in[3] >> 7"},
        {Ps2InputType.L2, "in[4] >> 0"},
        {Ps2InputType.R2, "in[4] >> 1"},
        {Ps2InputType.L1, "in[4] >> 2"},
        {Ps2InputType.R1, "in[4] >> 3"},
        {Ps2InputType.Triangle, "in[4] >> 4"},
        {Ps2InputType.Circle, "in[4] >> 5"},
        {Ps2InputType.Cross, "in[4] >> 6"},
        {Ps2InputType.Square, "in[4] >> 7"}
    };

    public Ps2Input(Ps2InputType input)
    {
        Input = input;
    }

    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        return Mappings[Input];
    }

    public bool IsAnalog => Input <= Ps2InputType.Dualshock2R2;

    public bool RequiresSpi()
    {
        return true;
    }

    public bool RequiresI2C()
    {
        return false;
    }

    public string GenerateAll(bool xbox, List<Tuple<IInput, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        //TODO: something like wii
        //Though, PS2 controller bindings are interesting as we are able to filter out to just the ones we actually use
        //so, we need to replace generated with whatever the id ends up actually being, based on the Dualshock2Order
        return "";
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_PS2"};
    }
}