using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class DjInput : TwiInput
{
    public static readonly string DjTwiType = "dj";
    public static readonly int DjTwiFreq = 150000;

    public DjInput(DjInputType input, ConfigViewModel model, bool smoothing, int sda = -1,
        int scl = -1, bool combined = false) : base(
        DjTwiType, DjTwiFreq, sda, scl, model)
    {
        Smoothing = smoothing;
        Combined = combined;
        BindableTwi = !combined && Model.Microcontroller.TwiAssignable;
        Input = input;
        IsAnalog = Input <= DjInputType.RightTurntable;
    }

    public bool Combined { get; }
    [Reactive]
    public bool Smoothing { get; set; }

    public bool BindableTwi { get; }

    public DjInputType Input { get; set; }
    public override InputType? InputType => Types.InputType.TurntableInput;

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => false;
    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate()
    {
        switch (Input)
        {
            case DjInputType.LeftTurntable:
                return "(dj_turntable_left)";
            case DjInputType.RightTurntable:
                return "(dj_turntable_right)";
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
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] usbHostInputsRaw, byte[] usbHostRaw)
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
        if (mode is not (ConfigField.Ps3 or ConfigField.Shared or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Ps4))
            return "";
        var left = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Left"))
                .Select(binding => binding.Item2));
        var right = string.Join(";",
            bindings.Where(binding => (binding.Item1 as DjInput)!.Input.ToString().Contains("Right"))
                .Select(binding => binding.Item2));
        return $@"if (djLeftValid) {{
                    {left}
                  }} 
                  if (djRightValid) {{
                    {right}
                  }}";
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        var list = new List<string>(base.RequiredDefines()) {"INPUT_DJ_TURNTABLE"};
        if (Smoothing && Input is DjInputType.LeftTurntable)
        {
            list.Add("INPUT_DJ_TURNTABLE_SMOOTHING_LEFT");
        }
        if (Smoothing && Input is DjInputType.RightTurntable)
        {
            list.Add("INPUT_DJ_TURNTABLE_SMOOTHING_RIGHT");
        }

        return list;
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedDjInputCombined(Input);

        return new SerializedDjInput(Sda, Scl, Input, Smoothing);
    }
}