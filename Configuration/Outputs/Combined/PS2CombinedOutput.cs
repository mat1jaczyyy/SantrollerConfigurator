using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class Ps2CombinedOutput : CombinedSpiOutput
{
    public static readonly Dictionary<Ps2InputType, StandardButtonType> Buttons = new()
    {
        {Ps2InputType.Cross, StandardButtonType.A},
        {Ps2InputType.Circle, StandardButtonType.B},
        {Ps2InputType.Square, StandardButtonType.X},
        {Ps2InputType.Triangle, StandardButtonType.Y},
        {Ps2InputType.L2, StandardButtonType.Lb},
        {Ps2InputType.R2, StandardButtonType.Rb},
        {Ps2InputType.L3, StandardButtonType.LeftStick},
        {Ps2InputType.R3, StandardButtonType.RightStick},
        {Ps2InputType.Select, StandardButtonType.Select},
        {Ps2InputType.Start, StandardButtonType.Start},
        {Ps2InputType.Down, StandardButtonType.Down},
        {Ps2InputType.Up, StandardButtonType.Up},
        {Ps2InputType.Left, StandardButtonType.Left},
        {Ps2InputType.Right, StandardButtonType.Right},
        {Ps2InputType.GuitarGreen, StandardButtonType.A},
        {Ps2InputType.GuitarRed, StandardButtonType.B},
        {Ps2InputType.GuitarYellow, StandardButtonType.Y},
        {Ps2InputType.GuitarBlue, StandardButtonType.X},
        {Ps2InputType.GuitarOrange, StandardButtonType.Lb},
        {Ps2InputType.GuitarStrumDown, StandardButtonType.Down},
        {Ps2InputType.GuitarStrumUp, StandardButtonType.Up},
        {Ps2InputType.GuitarSelect, StandardButtonType.Select},
        {Ps2InputType.GuitarStart, StandardButtonType.Start},
        {Ps2InputType.NegConR, StandardButtonType.Rb},
        {Ps2InputType.NegConA, StandardButtonType.B},
        {Ps2InputType.NegConB, StandardButtonType.Y},
        {Ps2InputType.NegConStart, StandardButtonType.Start},
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
        {Ps2InputType.NegConL, StandardAxisType.LeftTrigger},
    };


    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

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

    private readonly Microcontroller _microcontroller;

    private readonly AvaloniaList<Output> _outputs = new();
    public List<int> AvailablePins => Microcontroller.GetAllPins();

    public Ps2CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? miso = null, int? mosi = null,
        int? sck = null, int? att = null, int? ack = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, microcontroller, Ps2Input.Ps2SpiType,
        Ps2Input.Ps2SpiFreq, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, "PS2", miso, mosi, sck)
    {
        _microcontroller = microcontroller;
        _ackConfig = microcontroller
            .GetOrSetPin(Ps2Input.Ps2AckType, ack ?? microcontroller.SupportedAckPins()[0], DevicePinMode.Floating);
        _attConfig = microcontroller.GetOrSetPin(Ps2Input.Ps2AttType, att ?? 0, DevicePinMode.Output);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));
        microcontroller.PinConfigs.CollectionChanged +=
            (_, _) => this.RaisePropertyChanged(nameof(AvailablePins));
        if (outputs != null)
        {
            _outputs = new AvaloniaList<Output>(outputs);
        }
        else
        {
            CreateDefaults();
        }
    }

    public void CreateDefaults()
    {
        _outputs.Clear();
        foreach (var pair in Buttons)
        {
            _outputs.Add(new ControllerButton(Model,
                new Ps2Input(pair.Key, _microcontroller, Miso, Mosi, Sck, Att, Ack, combined:true),
                Colors.Transparent,
                Colors.Transparent, null,
                10,
                pair.Value));
        }

        _outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConI, _microcontroller, Miso, Mosi, Sck, Att, Ack, combined:true),
                AnalogToDigitalType.Trigger, 128),
            Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.A));
        _outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConIi, _microcontroller, Miso, Mosi, Sck, Att, Ack, combined:true),
                AnalogToDigitalType.Trigger, 128),
            Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.X));
        _outputs.Add(new ControllerButton(Model,
            new AnalogToDigital(new Ps2Input(Ps2InputType.NegConL, _microcontroller, Miso, Mosi, Sck, Att, Ack, combined:true),
                AnalogToDigitalType.Trigger, 240),
            Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Lb));

        foreach (var pair in Axis)
        {
            _outputs.Add(new ControllerAxis(Model, new Ps2Input(pair.Key, _microcontroller, Miso, Mosi, Sck, Att, Ack, combined:true),
                Colors.Transparent,
                Colors.Transparent, null, 1, 0, 0, pair.Value));
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPs2CombinedOutput(Miso, Mosi, Sck, Att, Ack, _outputs.ToList());
    }

    public override AvaloniaList<Output> Outputs => _outputs;
    

    private Ps2ControllerType? _detectedType;
    public string? DetectedType => _detectedType?.ToString() ?? "None";


    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType,
            wiiControllerType);
        if (!ps2ControllerType.Any()) return;
        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type)) return;
        var newType = (Ps2ControllerType)type;
        if (newType == _detectedType) return;
        this.RaisePropertyChanging(nameof(DetectedType));
        _detectedType = newType;
        this.RaisePropertyChanged(nameof(DetectedType));

    }
}