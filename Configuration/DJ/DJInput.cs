using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.DJ;

public class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public bool Combined { get; }

    public DjInput(DjInputType input, Microcontroller microcontroller, int? sda = null, int? scl = null, bool combined = false) : base(
        microcontroller, DjTwiType, DjTwiFreq, sda, scl)
    {
        Combined = combined;
        Input = input;
    }

    public DjInputType Input { get; set; }
    public override InputType? InputType => Types.InputType.TurntableInput;

    public override string Generate()
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                return "((int8_t)dj_left[2])";
            case DjInputType.RightTurnable:
                return "((int8_t)dj_right[2])";
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

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        if (!djLeftRaw.Any())
        {
            djLeftRaw = new byte[] {0, 0, 0};
        }
        if (!djRightRaw.Any())
        {
            djRightRaw = new byte[] {0, 0, 0};
        }
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                RawValue = (sbyte) djLeftRaw[2];
                break;
            case DjInputType.RightTurnable:
                RawValue = (sbyte) djRightRaw[2];
                break;
            case DjInputType.LeftAny:
                RawValue = djLeftRaw[0] != 0 ? 1 : 0;
                break;
            case DjInputType.RightAny:
                RawValue = djRightRaw[0] != 0 ? 1 : 0;
                break;
            case DjInputType.LeftBlue:
            case DjInputType.LeftGreen:
            case DjInputType.LeftRed:
                RawValue = (djLeftRaw[0] & 1 << ((byte) Input - (byte) DjInputType.LeftGreen + 4)) != 0 ? 1 : 0;
                break;
            case DjInputType.RightGreen:
            case DjInputType.RightRed:
            case DjInputType.RightBlue:
                RawValue = (djRightRaw[0] & 1 << ((byte) Input - (byte) DjInputType.RightGreen + 4)) != 0 ? 1 : 0;
                break;
        }
    }

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

    public override SerializedInput Serialise()
    {
        if (Combined)
        {
            return new SerializedDjInputCombined(Input);
        }
        return new SerializedDjInput(Sda, Scl, Input);
    }
}