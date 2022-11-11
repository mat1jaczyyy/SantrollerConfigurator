using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.DJ;

public class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public DjInput(DjInputType input, Microcontroller microcontroller, int? sda = null, int? scl = null) : base(
        microcontroller, DjTwiType, DjTwiFreq, sda, scl)
    {
        Input = input;
    }

    public DjInputType Input { get; set; }
    public override InputType? InputType => Types.InputType.TurntableInput;

    public override string Generate()
    {
        switch (Input)
        {
            //TODO: would it make more sense to drop this 13 here, and then have a default multiplier that we apply to turntables?
            case DjInputType.LeftTurntable:
                return "((int8_t)dj_left[2]) << 13";
            case DjInputType.RightTurnable:
                return "((int8_t)dj_right[2]) << 13";
            case DjInputType.LeftAny:
                return "dj_left[0]";
            case DjInputType.RightAny:
                return "dj_right[0]";
            case DjInputType.LeftBlue:
            case DjInputType.LeftGreen:
            case DjInputType.LeftRed:
                return $"(dj_left[0] & {1 << ((byte) Input - (byte) DjInputType.LeftGreen + 4)})";
            case DjInputType.RightGreen:
            case DjInputType.RightRed:
            case DjInputType.RightBlue:
                return $"(dj_right[0] & {1 << ((byte) Input - (byte) DjInputType.RightGreen + 4)})";
        }

        throw new InvalidOperationException("Shouldn't get here!");
    }

    public override bool IsAnalog => Input <= DjInputType.RightTurnable;

    public override string GenerateAll(List<Tuple<Input, string>> bindings)
    {
        var left = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Left"))
                .Select(binding => binding.Item2));
        var right = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Right"))
                .Select(binding => binding.Item2));
        return $"if (djLeftValid) {{{left}}} if (djRightValid) {{{right}}}";
    }

    public override List<DevicePin> Pins => new();
    public override bool IsUint => false;

    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_DJ_TURNTABLE"}).ToList();
    }

    public override SerializedInput GetJson()
    {
        return new SerializedDjInput(Sda, Scl, Input);
    }
}