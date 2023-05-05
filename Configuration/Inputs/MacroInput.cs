using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class MacroInput : Input
{
    public MacroInput(Input child1, Input child2,
        ConfigViewModel model) : base(model)
    {
        Child1 = child1;
        Child2 = child2;
        this.WhenAnyValue(x => x.Child1, x => x.Child2)
            .Select(x => x.Item1.RawValue > 0 && x.Item2.RawValue > 0 ? 1 : 0).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(s => RawValue = s);
        IsAnalog = false;
        this.WhenAnyValue(x => x.Child1).Select(x => x.InnermostInput() is DjInput)
            .ToPropertyEx(this, x => x.IsDj1);
        this.WhenAnyValue(x => x.Child1).Select(x => x.InnermostInput() is WiiInput)
            .ToPropertyEx(this, x => x.IsWii1);
        this.WhenAnyValue(x => x.Child1).Select(x => x.InnermostInput() is Ps2Input)
            .ToPropertyEx(this, x => x.IsPs21);
        this.WhenAnyValue(x => x.Child2).Select(x => x.InnermostInput() is DjInput)
            .ToPropertyEx(this, x => x.IsDj2);
        this.WhenAnyValue(x => x.Child2).Select(x => x.InnermostInput() is WiiInput)
            .ToPropertyEx(this, x => x.IsWii2);
        this.WhenAnyValue(x => x.Child2).Select(x => x.InnermostInput() is Ps2Input)
            .ToPropertyEx(this, x => x.IsPs22);
    }

    public InputType? SelectedInputType1
    {
        get => Child1.InputType;
        set => SetInput(value, true, null, null, null, null, null);
    }

    public WiiInputType WiiInputType1
    {
        get => (Child1.InnermostInput() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType1, true, value, null, null, null, null);
    }

    public Ps2InputType Ps2InputType1
    {
        get => (Child1.InnermostInput() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType1, true, null, value, null, null, null);
    }

    public DjInputType DjInputType1
    {
        get => (Child1.InnermostInput() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType1, true, null, null, null, null, value);
    }

    public InputType? SelectedInputType2
    {
        get => Child2.InputType;
        set => SetInput(value, false, null, null, null, null, null);
    }

    public WiiInputType WiiInputType2
    {
        get => (Child2.InnermostInput() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType1, false, value, null, null, null, null);
    }

    public Ps2InputType Ps2InputType2
    {
        get => (Child2.InnermostInput() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType1, false, null, value, null, null, null);
    }

    public DjInputType DjInputType2
    {
        get => (Child2.InnermostInput() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType1, false, null, null, null, null, value);
    }

    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsDj1 { get; }
    [ObservableAsProperty] public bool IsWii1 { get; }
    [ObservableAsProperty] public bool IsPs21 { get; }
    [ObservableAsProperty] public bool IsDj2 { get; }
    [ObservableAsProperty] public bool IsWii2 { get; }

    [ObservableAsProperty] public bool IsPs22 { get; }
    // ReSharper enable UnassignedGetOnlyAutoProperty


    private void SetInput(InputType? inputType, bool isChild1, WiiInputType? wiiInput, Ps2InputType? ps2InputType,
        GhWtInputType? ghWtInputType, Gh5NeckInputType? gh5NeckInputType, DjInputType? djInputType)
    {
        var child = isChild1 ? Child1 : Child2;
        var lastPin = inputType is Types.InputType.AnalogPinInput or Types.InputType.MultiplexerInput
            ? Model.Microcontroller.GetFirstAnalogPin()
            : 0;
        var pinMode = DevicePinMode.PullUp;
        if (child.InnermostInput() is DirectInput direct)
        {
            if (direct.IsAnalog || inputType != Types.InputType.AnalogPinInput)
            {
                lastPin = direct.Pin;
                if (!direct.IsAnalog) pinMode = direct.PinMode;
            }
        }


        Input input;
        switch (inputType)
        {
            case Types.InputType.AnalogPinInput:
                input = new DirectInput(lastPin, DevicePinMode.Analog, Model);
                break;
            case Types.InputType.MultiplexerInput:
                input = new MultiplexerInput(lastPin, 0, 0, 0, 0, 0, MultiplexerType.EightChannel, Model);
                break;
            case Types.InputType.MacroInput:
                input = new MacroInput(new DirectInput(lastPin, pinMode, Model),
                    new DirectInput(lastPin, pinMode, Model), Model);
                break;
            case Types.InputType.DigitalPinInput:
                input = new DirectInput(lastPin, pinMode, Model);
                break;
            case Types.InputType.TurntableInput when child.InnermostInput() is not DjInput:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model);
                break;
            case Types.InputType.TurntableInput when child.InnermostInput() is DjInput dj:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model, dj.Sda, dj.Scl);
                break;
            case Types.InputType.Gh5NeckInput when child.InnermostInput() is not Gh5NeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model);
                break;
            case Types.InputType.Gh5NeckInput when child.InnermostInput() is Gh5NeckInput gh5:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, gh5.Sda, gh5.Scl);
                break;
            case Types.InputType.WtNeckInput when child.InnermostInput() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model, Model.Microcontroller.GetFirstAnalogPin(),
                    Model.Microcontroller.GetFirstDigitalPin(), Model.Microcontroller.GetFirstDigitalPin(),
                    Model.Microcontroller.GetFirstDigitalPin());
                break;
            case Types.InputType.WtNeckInput when child.InnermostInput() is GhWtTapInput wt:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model, wt.Pin, wt.PinS0, wt.PinS1, wt.PinS2);
                break;
            case Types.InputType.WiiInput when child.InnermostInput() is not WiiInput:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model);
                break;
            case Types.InputType.WiiInput when child.InnermostInput() is WiiInput wii:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model, wii.Sda, wii.Scl);
                break;
            case Types.InputType.Ps2Input when child.InnermostInput() is not Ps2Input:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model);
                break;
            case Types.InputType.Ps2Input when child.InnermostInput() is Ps2Input ps2:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model, ps2.Miso, ps2.Mosi, ps2.Sck,
                    ps2.Att,
                    ps2.Ack);
                break;
            default:
                return;
        }

        if (input.IsAnalog)
        {
            input = new AnalogToDigital(input, input.IsUint ? AnalogToDigitalType.Trigger : AnalogToDigitalType.JoyLow,
                input.IsUint ? ushort.MaxValue / 2 : short.MaxValue / 2, Model);
        }

        if (isChild1)
        {
            Child1 = input;
        }
        else
        {
            Child2 = input;
        }
    }


    public IEnumerable<GhWtInputType> GhWtInputTypes => Enum.GetValues<GhWtInputType>();

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes => Enum.GetValues<Gh5NeckInputType>();

    public IEnumerable<object> KeyOrMouseInputs => Enum.GetValues<MouseButtonType>().Cast<object>()
        .Concat(Enum.GetValues<MouseAxisType>().Cast<object>()).Concat(KeyboardButton.Keys.Keys.Cast<object>());

    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes =>
        Enum.GetValues<WiiInputType>().OrderBy(s => EnumToStringConverter.Convert(s));

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();

    public IEnumerable<InputType> InputTypes => new[]
    {
        Types.InputType.AnalogPinInput, Types.InputType.DigitalPinInput, Types.InputType.WiiInput,
        Types.InputType.Ps2Input, Types.InputType.TurntableInput
    };


    [Reactive] public Input Child1 { get; set; }
    [Reactive] public Input Child2 { get; set; }
    public override InputType? InputType => Types.InputType.MacroInput;

    public override IList<DevicePin> Pins => Child1.Pins.Concat(Child2.Pins).ToList();
    public override IList<PinConfig> PinConfigs => Child1.PinConfigs.Concat(Child2.PinConfigs).ToList();

    public override bool IsUint => false;


    public override string Generate(ConfigField mode)
    {
        return $"{Child1.Generate(mode)} && {Child2.Generate(mode)}";
    }

    public override string Title => "Macro";

    public override SerializedInput Serialise()
    {
        return new SerializedMacroInput(Child1.Serialise(), Child2.Serialise());
    }

    public override Input InnermostInput()
    {
        return Child1;
    }

    public override IList<Input> Inputs()
    {
        return new List<Input> {Child1, Child2};
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child1.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
        Child2.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on MacroInput, call it on its children");
    }

    public override void Dispose()
    {
        Child1.Dispose();
        Child2.Dispose();
    }

    public override string GetImagePath()
    {
        return "";
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child1.RequiredDefines().Concat(Child2.RequiredDefines()).ToList();
    }
}