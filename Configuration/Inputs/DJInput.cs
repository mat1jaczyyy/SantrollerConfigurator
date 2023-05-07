using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public DjInput(DjInputType input, ConfigViewModel model, int sda = -1,
        int scl = -1, bool combined = false) : base(
        DjTwiType, DjTwiFreq, sda, scl, model)
    {
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable;
        Input = input;
        IsAnalog = Input <= DjInputType.RightTurntable;
    }

    public bool Combined { get; }

    public bool BindableTwi { get; }

    public DjInputType Input { get; set; }
    public override InputType? InputType => Types.InputType.TurntableInput;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => false;
    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate(ConfigField mode)
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                return "((int8_t)dj_left[2])";
            case DjInputType.RightTurntable:
                return "((int8_t)dj_right[2])";
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

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable when djLeftRaw.Any():
                RawValue = (sbyte) djLeftRaw[2];
                break;
            case DjInputType.RightTurntable when djRightRaw.Any():
                RawValue = (sbyte) djRightRaw[2];
                break;
            case DjInputType.LeftBlue when djLeftRaw.Any():
            case DjInputType.LeftGreen when djLeftRaw.Any():
            case DjInputType.LeftRed when djLeftRaw.Any():
                RawValue = (djLeftRaw[0] & (1 << ((byte) Input - (byte) DjInputType.LeftGreen + 4))) != 0 ? 1 : 0;
                break;
            case DjInputType.RightGreen when djRightRaw.Any():
            case DjInputType.RightRed when djRightRaw.Any():
            case DjInputType.RightBlue when djRightRaw.Any():
                RawValue = (djRightRaw[0] & (1 << ((byte) Input - (byte) DjInputType.RightGreen + 4))) != 0 ? 1 : 0;
                break;
        }
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        var left = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Left"))
                .Select(binding => binding.Item2));
        var right = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Right"))
                .Select(binding => binding.Item2));
        var leftTrigger = mode == ConfigField.Shared ? "" : Output.GetReportField(StandardAxisType.LeftTrigger) + "=0;";
        var rightTrigger = mode == ConfigField.Shared
            ? ""
            : Output.GetReportField(StandardAxisType.RightTrigger) + "=0;";
        return $@"if (djLeftValid) {{
                    {leftTrigger}
                    {left}
                  }} 
                  if (djRightValid) {{
                    {rightTrigger}
                    {right}
                  }}";
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return base.RequiredDefines().Concat(new[] {"INPUT_DJ_TURNTABLE"}).ToList();
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedDjInputCombined(Input);

        return new SerializedDjInput(Sda, Scl, Input);
    }
}