using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class GhwtCombinedOutput : CombinedOutput
{
    private static readonly Dictionary<GhWtInputType, StandardButtonType> Taps = new()
    {
        {GhWtInputType.TapGreen, StandardButtonType.A},
        {GhWtInputType.TapRed, StandardButtonType.B},
        {GhWtInputType.TapYellow, StandardButtonType.Y},
        {GhWtInputType.TapBlue, StandardButtonType.X},
        {GhWtInputType.TapOrange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<GhWtInputType, InstrumentButtonType> TapRb = new()
    {
        {GhWtInputType.TapGreen, InstrumentButtonType.SoloGreen},
        {GhWtInputType.TapRed, InstrumentButtonType.SoloRed},
        {GhWtInputType.TapYellow, InstrumentButtonType.SoloYellow},
        {GhWtInputType.TapBlue, InstrumentButtonType.SoloBlue},
        {GhWtInputType.TapOrange, InstrumentButtonType.SoloOrange}
    };


    private readonly DirectPinConfig _pin;
    private readonly DirectPinConfig _pinConfigS0;
    private readonly DirectPinConfig _pinConfigS1;
    private readonly DirectPinConfig _pinConfigS2;

    public GhwtCombinedOutput(ConfigViewModel model, int pin = -1, int pinS0 = -1, int pinS1 = -1,
        int pinS2 = -1) : base(model)
    {
        Outputs.Clear();
        _pin = Model.GetPinForType(GhWtTapInput.GhWtAnalogPinType, pin, DevicePinMode.PullUp);
        _pinConfigS0 = Model.GetPinForType(GhWtTapInput.GhWtS0PinType, pinS0, DevicePinMode.Output);
        _pinConfigS1 = Model.GetPinForType(GhWtTapInput.GhWtS1PinType, pinS1, DevicePinMode.Output);
        _pinConfigS2 = Model.GetPinForType(GhWtTapInput.GhWtS2PinType, pinS2, DevicePinMode.Output);
        this.WhenAnyValue(x => x._pin.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
        this.WhenAnyValue(x => x._pinConfigS0.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS0)));
        this.WhenAnyValue(x => x._pinConfigS1.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS1)));
        this.WhenAnyValue(x => x._pinConfigS2.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(PinS2)));
        this.WhenAnyValue(x => x.Model.WtSensitivity).Subscribe(_ => this.RaisePropertyChanged(nameof(Sensitivity)));
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    public int Pin
    {
        get => _pin.Pin;
        set => _pin.Pin = value;
    }

    public int PinS0
    {
        get => _pinConfigS0.Pin;
        set => _pinConfigS0.Pin = value;
    }

    public byte Sensitivity
    {
        get => Model.WtSensitivity;
        set => Model.WtSensitivity = value;
    }

    public int PinS1
    {
        get => _pinConfigS1.Pin;
        set => _pinConfigS1.Pin = value;
    }

    public int PinS2
    {
        get => _pinConfigS2.Pin;
        set => _pinConfigS2.Pin = value;
    }


    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(true);

    public List<int> AvailablePinsDigital => Model.Microcontroller.GetAllPins(false);

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return Resources.GhwtCombinedTitle;
    }

    public override Enum GetOutputType()
    {
        return SimpleType.WtNeckSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Add(new ControllerButton(Model,
            new GhWtTapInput(GhWtInputType.TapAll, Model, Pin, PinS0, PinS1, PinS2,
                true), Colors.Black,
            Colors.Black, Array.Empty<byte>(), 5, StandardButtonType.A, true));
        Outputs.Add(new GuitarAxis(Model,
            new GhWtTapInput(GhWtInputType.TapBar, Model, Pin, PinS0, PinS1, PinS2,
                true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
            GuitarAxisType.Slider, true));
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var tapAnalog =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: GhWtTapInput {Input: GhWtInputType.TapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: GhWtTapInput {Input: GhWtInputType.TapAll}});
        if (tapAnalog == null && tapFrets == null) return Outputs.Items;
        var outputs = new List<Output>(Outputs.Items);
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceControllerType is DeviceControllerType.RockBandGuitar)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model,
                new GhWtTapInput(pair.Key, Model, Pin, PinS0, PinS1, PinS2, true), Colors.Black, Colors.Black,
                Array.Empty<byte>(), 5, pair.Value, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        foreach (var pair in Taps)
        {
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(pair.Key, Model, Pin, PinS0, PinS1, PinS2, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), 5, pair.Value, true));
            outputs.Remove(tapFrets);
        }

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGhwtCombinedOutput(Pin, PinS0, PinS1, PinS2, Outputs.Items.ToList());
    }

    public override void UpdateBindings()
    {
        var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
        var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
        if (Model.DeviceControllerType.Is5FretGuitar())
        {
            if (axisController == null) return;
            Outputs.Remove(axisController);
            Outputs.Add(new GuitarAxis(Model,
                new GhWtTapInput(GhWtInputType.TapBar, Model, Pin, PinS0, PinS1, PinS2,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                GuitarAxisType.Slider, true));
        }
        else if (axisGuitar != null)
        {
            Outputs.Remove(axisGuitar);
            Outputs.Add(new ControllerAxis(Model,
                new GhWtTapInput(GhWtInputType.TapBar, Model, Pin, PinS0, PinS1, PinS2,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                ushort.MaxValue, StandardAxisType.LeftStickX, true));
        }
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return new PinConfig[] {_pin, _pinConfigS0, _pinConfigS1, _pinConfigS2};
    }
}