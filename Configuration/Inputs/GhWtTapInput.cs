using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class GhWtTapInput : Input
{
    public static string GhWtAnalogPinType = "ghwt";
    public static string GhWtS0PinType = "ghwts0";
    public static string GhWtS1PinType = "ghwts1";
    public static string GhWtS2PinType = "ghwts2";

    private readonly Dictionary<GhWtInputType, int> _order = new()
    {
        {GhWtInputType.TapGreen, 0},
        {GhWtInputType.TapRed, 1},
        {GhWtInputType.TapYellow, 2},
        {GhWtInputType.TapBlue, 3},
        {GhWtInputType.TapOrange, 4}
    };

    public GhWtTapInput(GhWtInputType input, ConfigViewModel model, int pinInput, int pinS0, int pinS1, int pinS2,
        bool combined = false) : base(model)
    {
        Combined = combined;
        Input = input;
        IsAnalog = input is GhWtInputType.TapBar;
        PinConfigAnalog = Model.GetPinForType(GhWtAnalogPinType, pinInput, DevicePinMode.PullUp);
        PinConfigS0 = Model.GetPinForType(GhWtS0PinType, pinS0, DevicePinMode.Output);
        PinConfigS1 = Model.GetPinForType(GhWtS1PinType, pinS1, DevicePinMode.Output);
        PinConfigS2 = Model.GetPinForType(GhWtS2PinType, pinS2, DevicePinMode.Output);
        this.WhenAnyValue(x => x.PinConfigAnalog.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
        this.WhenAnyValue(x => x.PinConfigS0.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS0)));
        this.WhenAnyValue(x => x.PinConfigS1.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS1)));
        this.WhenAnyValue(x => x.PinConfigS2.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS2)));
        this.WhenAnyValue(x => x.Model.WtSensitivity).Subscribe(_ => this.RaisePropertyChanged(nameof(Sensitivity)));
    }

    public DirectPinConfig PinConfigAnalog { get; }
    private DirectPinConfig PinConfigS0 { get; }
    private DirectPinConfig PinConfigS1 { get; }
    private DirectPinConfig PinConfigS2 { get; }


    public int Pin
    {
        get => PinConfigAnalog.Pin;
        set
        {
            PinConfigAnalog.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS0
    {
        get => PinConfigS0.Pin;
        set
        {
            PinConfigS0.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS1
    {
        get => PinConfigS1.Pin;
        set
        {
            PinConfigS1.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public int PinS2
    {
        get => PinConfigS2.Pin;
        set
        {
            PinConfigS2.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public override IList<PinConfig> PinConfigs => new List<PinConfig>
        {PinConfigAnalog, PinConfigS0, PinConfigS1, PinConfigS2};


    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(true);
    public List<int> AvailablePinsDigital => Model.Microcontroller.GetAllPins(false);

    public GhWtInputType Input { get; set; }
    public bool Combined { get; }

    public int Sensitivity
    {
        get => 60 - Model.WtSensitivity;
        set => Model.WtSensitivity = (byte) (60 - value);
    }

    public override string Title => EnumToStringConverter.Convert(Input);

    public override InputType? InputType => Types.InputType.WtNeckInput;
    public override bool IsUint => true;

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(PinConfigAnalog.Pin, DevicePinMode.Floating)
    };

    public override string Generate()
    {
        return Input == GhWtInputType.TapBar ? "gh5_mapping[rawWt]" : $"(rawWt & {1 << (byte) Input})";
    }

    public override SerializedInput Serialise()
    {
        if (Combined) return new SerializedGhWtInputCombined(Input);
        return new SerializedGhWtInput(PinConfigAnalog.Pin, PinS0, PinS1, PinS2, Input);
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, ReadOnlySpan<byte> ps2Raw,
        ReadOnlySpan<byte> wiiRaw, ReadOnlySpan<byte> djLeftRaw,
        ReadOnlySpan<byte> djRightRaw, ReadOnlySpan<byte> gh5Raw, ReadOnlySpan<byte> ghWtRaw,
        ReadOnlySpan<byte> ps2ControllerType, ReadOnlySpan<byte> wiiControllerType,
        ReadOnlySpan<byte> usbHostInputsRaw, ReadOnlySpan<byte> usbHostRaw)
    {
        if (ghWtRaw.IsEmpty) return;
        RawValue = BitConverter.ToInt32(ghWtRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[]
            {"INPUT_WT_NECK", $"WT_PIN_INPUT {Pin}", $"WT_PIN_S0 {PinS0}", $"WT_PIN_S1 {PinS1}", $"WT_PIN_S2 {PinS2}"};
    }
}