using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration.DJ;

public class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 250000;

    public DjInput(DjInputType input, Microcontroller microcontroller, int? sda = null, int? scl = null) : base(
        microcontroller, DjTwiType, DjTwiFreq, sda, scl)
    {
        Input = input;
    }

    public DjInputType Input { get; set; }

    public override string Generate()
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                return "((int8_t)dj_left[2]) << 5";
            case DjInputType.RightTurnable:
                return "((int8_t)dj_right[2]) << 5";
            case DjInputType.LeftAny:
                return "dj_left[0]";
            case DjInputType.RightAny:
                return "dj_right[0]";
            case DjInputType.LeftBlue:
            case DjInputType.LeftGreen:
            case DjInputType.LeftRed:
                return $"(dj_left[0] & {1 << ((byte) Input) - ((byte) DjInputType.LeftGreen)})";
            case DjInputType.RightGreen:
            case DjInputType.RightRed:
            case DjInputType.RightBlue:
                return $"(dj_right[0] & {1 << ((byte) Input) - ((byte) DjInputType.RightGreen)})";
        }

        throw new InvalidOperationException("Shouldn't get here!");
    }

    public override bool IsAnalog => Input <= DjInputType.RightTurnable;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller)
    {
        string left = String.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Left"))
                .Select(binding => binding.Item2));
        string right = String.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Right"))
                .Select(binding => binding.Item2));
        return $"if (djLeftValid) {{{left}}} if (djRightValid) {{{right}}}";
    }

    public override List<DevicePin> Pins => new();

    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_DJ_TURNTABLE"}).ToList();
    }

    public override SerializedInput GetJson()
    {
        return new SerializedDjInput(Sda, Scl, Input);
    }
}