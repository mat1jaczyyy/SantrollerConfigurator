using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class Ps2CombinedOutput : CombinedSpiOutput
{
    public static readonly Dictionary<Ps2InputType, StandardButtonType> Buttons = new()
    {
        {Ps2InputType.Cross, StandardButtonType.A},
        {Ps2InputType.Circle, StandardButtonType.B},
        {Ps2InputType.Square, StandardButtonType.X},
        {Ps2InputType.Triangle, StandardButtonType.Y},
        {Ps2InputType.L2, StandardButtonType.LeftShoulder},
        {Ps2InputType.R2, StandardButtonType.RightShoulder},
        {Ps2InputType.L3, StandardButtonType.LeftThumbClick},
        {Ps2InputType.R3, StandardButtonType.RightThumbClick},
        {Ps2InputType.Select, StandardButtonType.Back},
        {Ps2InputType.Start, StandardButtonType.Start},
        {Ps2InputType.DPadDown, StandardButtonType.DpadDown},
        {Ps2InputType.DPadUp, StandardButtonType.DpadUp},
        {Ps2InputType.DPadLeft, StandardButtonType.DpadLeft},
        {Ps2InputType.DPadRight, StandardButtonType.DpadRight},
        {Ps2InputType.GuitarGreen, StandardButtonType.A},
        {Ps2InputType.GuitarRed, StandardButtonType.B},
        {Ps2InputType.GuitarYellow, StandardButtonType.Y},
        {Ps2InputType.GuitarBlue, StandardButtonType.X},
        {Ps2InputType.GuitarOrange, StandardButtonType.LeftShoulder},
        {Ps2InputType.GuitarStrumDown, StandardButtonType.DpadDown},
        {Ps2InputType.GuitarStrumUp, StandardButtonType.DpadUp},
        {Ps2InputType.GuitarSelect, StandardButtonType.Back},
        {Ps2InputType.GuitarStart, StandardButtonType.Start},
        {Ps2InputType.NegConR, StandardButtonType.RightShoulder},
        {Ps2InputType.NegConA, StandardButtonType.B},
        {Ps2InputType.NegConB, StandardButtonType.Y},
        {Ps2InputType.NegConStart, StandardButtonType.Start}
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
        {Ps2InputType.NegConL, StandardAxisType.LeftTrigger}
    };

    public static readonly Dictionary<Ps2InputType, Ps3AxisType> Ps3Axis = new()
    {
        {Ps2InputType.Dualshock2UpButton, Ps3AxisType.PressureDpadUp},
        {Ps2InputType.Dualshock2RightButton, Ps3AxisType.PressureDpadRight},
        {Ps2InputType.Dualshock2LeftButton, Ps3AxisType.PressureDpadLeft},
        {Ps2InputType.Dualshock2DownButton, Ps3AxisType.PressureDpadDown},
        {Ps2InputType.Dualshock2L1, Ps3AxisType.PressureL1},
        {Ps2InputType.Dualshock2R1, Ps3AxisType.PressureR1},
        {Ps2InputType.Dualshock2Triangle, Ps3AxisType.PressureTriangle},
        {Ps2InputType.Dualshock2Circle, Ps3AxisType.PressureCircle},
        {Ps2InputType.Dualshock2Cross, Ps3AxisType.PressureCross},
        {Ps2InputType.Dualshock2Square, Ps3AxisType.PressureSquare}
    };


    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2CombinedOutput(ConfigViewModel model, int miso = -1, int mosi = -1,
        int sck = -1, int att = -1, int ack = -1) : base(model, Ps2Input.Ps2SpiType,
        Ps2Input.Ps2SpiFreq, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, "PS2", miso, mosi, sck)
    {
        Outputs.Clear();
        _ackConfig = new DirectPinConfig(model, Ps2Input.Ps2AckType, ack, DevicePinMode.Floating);
        _attConfig = new DirectPinConfig(model, Ps2Input.Ps2AttType, att, DevicePinMode.Output);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));

        Outputs.Connect().Filter(x => x is OutputAxis)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType).Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType).Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    public int Ack
    {
        get => _ackConfig.Pin;
        set => _ackConfig.Pin = value;
    }

    public int Att
    {
        get => _attConfig.Pin;
        set => _attConfig.Pin = value;
    }

    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(false);

    [Reactive] public Ps2ControllerType DetectedType { get; set; }

    [Reactive] public bool ControllerFound { get; set; }

    public override IEnumerable<Output> ValidOutputs()
    {
        var outputs = base.ValidOutputs().ToList();
        var joyToDpad = outputs.FirstOrDefault(s => s is JoystickToDpad);
        if (joyToDpad?.Enabled != true) return outputs;
        outputs.Remove(joyToDpad);
        outputs.Add(joyToDpad.ValidOutputs());
        return outputs;
    }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    private static Func<Output, bool> CreateFilter((bool controllerFound, Ps2ControllerType controllerType) tuple)
    {
        return output => !tuple.controllerFound || output is JoystickToDpad ||
                         (output.Input.InnermostInput() is Ps2Input ps2Input &&
                          ps2Input.SupportsType(tuple.controllerType));
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "PS2 Controller Inputs";
    }

    public override object GetOutputType()
    {
        return SimpleType.Ps2InputSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
            Outputs.Add(new ControllerButton(Model,
                new Ps2Input(pair.Key, Model, Miso, Mosi, Sck, Att, Ack, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(),
                10,
                pair.Value, true));

        Outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConI, Model, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 128, Model),
            Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.A, true));
        Outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConIi, Model, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 128, Model),
            Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.X, true));
        Outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(
                new Ps2Input(Ps2InputType.NegConL, Model, Miso, Mosi, Sck, Att, Ack, true),
                AnalogToDigitalType.Trigger, 240, Model),
            Colors.Black, Colors.Black, Array.Empty<byte>(), 10, StandardButtonType.LeftShoulder, true));

        Outputs.Add(new ControllerAxis(Model,
            new DigitalToAnalog(
                new Ps2Input(Ps2InputType.GuitarTilt, Model, Miso, Mosi, Sck, Att, Ack,
                    true),
                Model), Colors.Black,
            Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
            0, StandardAxisType.RightStickY, true));
        foreach (var pair in Axis)
            if (pair.Value is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger ||
                pair.Key is Ps2InputType.GuitarWhammy)
                Outputs.Add(new ControllerAxis(Model,
                    new Ps2Input(pair.Key, Model, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue, 0, pair.Value, true));
            else
                Outputs.Add(new ControllerAxis(Model,
                    new Ps2Input(pair.Key, Model, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, pair.Value, true));

        Outputs.Add(new JoystickToDpad(Model, short.MaxValue / 2, false));
        UpdateBindings();
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPs2CombinedOutput(Miso, Mosi, Sck, Att, Ack, Outputs.Items.ToList());
    }


    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw);
        if (!ps2ControllerType.Any())
        {
            ControllerFound = false;
            return;
        }

        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type))
        {
            ControllerFound = false;
            return;
        }

        ControllerFound = true;
        var newType = (Ps2ControllerType) type;
        DetectedType = newType;
    }

    public override void UpdateBindings()
    {
        if (Model.DeviceType == DeviceControllerType.Guitar)
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
            {
                var items = Outputs.Items.Where(s => s is ControllerAxis {Type: StandardAxisType.RightStickX}).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Cast<ControllerAxis>().Select(item => new GuitarAxis(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    GuitarAxisType.Whammy, true)));
            }
        }
        else if (Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
        {
            var items = Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}).ToList();
            Outputs.RemoveMany(items);
            Outputs.AddRange(items.Cast<GuitarAxis>().Select(item => new ControllerAxis(Model, item.Input, item.LedOn,
                item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                StandardAxisType.RightStickX, true)));
        }

        InstrumentButtonTypeExtensions.ConvertBindings(Outputs, Model, true);

        switch (Model.DeviceType)
        {
            case DeviceControllerType.Guitar:
            {
                foreach (var output in Outputs.Items)
                {
                    if (output is GuitarButton guitarButton)
                    {
                        if (!InstrumentButtonTypeExtensions.LiveToGuitar.ContainsKey(guitarButton.Type)) continue;
                        Outputs.Remove(output);
                        Outputs.Add(new GuitarButton(Model, output.Input, output.LedOn, output.LedOff,
                            output.LedIndices.ToArray(), guitarButton.Debounce,
                            InstrumentButtonTypeExtensions.LiveToGuitar[guitarButton.Type], true));
                    }

                    if (output is not ControllerButton button) continue;
                    if (!InstrumentButtonTypeExtensions.GuitarMappings.ContainsKey(button.Type)) continue;
                    Outputs.Remove(output);
                    Outputs.Add(new GuitarButton(Model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), button.Debounce,
                        InstrumentButtonTypeExtensions.GuitarMappings[button.Type], true));
                }

                break;
            }
            case DeviceControllerType.LiveGuitar:
            {
                foreach (var output in Outputs.Items)
                {
                    if (output is GuitarButton guitarButton)
                    {
                        if (!InstrumentButtonTypeExtensions.GuitarToLive.ContainsKey(guitarButton.Type)) continue;
                        Outputs.Remove(output);
                        Outputs.Add(new GuitarButton(Model, output.Input, output.LedOn, output.LedOff,
                            output.LedIndices.ToArray(), guitarButton.Debounce,
                            InstrumentButtonTypeExtensions.GuitarToLive[guitarButton.Type], true));
                    }

                    if (output is not ControllerButton button) continue;
                    if (!InstrumentButtonTypeExtensions.LiveGuitarMappings.ContainsKey(button.Type)) continue;
                    Outputs.Remove(output);
                    Outputs.Add(new GuitarButton(Model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), button.Debounce,
                        InstrumentButtonTypeExtensions.LiveGuitarMappings[button.Type], true));
                }

                break;
            }
            default:
            {
                foreach (var output in Outputs.Items)
                {
                    if (output is not GuitarButton guitarButton) continue;
                    Outputs.Remove(output);
                    Outputs.Add(new ControllerButton(Model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), guitarButton.Debounce,
                        InstrumentButtonTypeExtensions.GuitarToStandard[guitarButton.Type], true));
                }

                break;
            }
        }

        if (Model.DeviceType == DeviceControllerType.Gamepad)
        {
            if (Outputs.Items.Any(s => s is Ps3Axis)) return;
            foreach (var pair in Ps3Axis)
                Outputs.Add(new Ps3Axis(Model,
                    new Ps2Input(pair.Key, Model, Miso, Mosi, Sck, Att, Ack, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, pair.Value));

            return;
        }

        Outputs.RemoveMany(Outputs.Items.Where(s => s is Ps3Axis));
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return new PinConfig[] {SpiConfig, _attConfig, _ackConfig};
    }
}