using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<Gh5NeckInputType, InstrumentButtonType> Taps = new()
    {
        {Gh5NeckInputType.TapGreen, InstrumentButtonType.Green},
        {Gh5NeckInputType.TapRed, InstrumentButtonType.Red},
        {Gh5NeckInputType.TapYellow, InstrumentButtonType.Yellow},
        {Gh5NeckInputType.TapBlue, InstrumentButtonType.Blue},
        {Gh5NeckInputType.TapOrange, InstrumentButtonType.Orange}
    };

    private static readonly Dictionary<Gh5NeckInputType, InstrumentButtonType> TapsRb = new()
    {
        {Gh5NeckInputType.TapGreen, InstrumentButtonType.SoloGreen},
        {Gh5NeckInputType.TapRed, InstrumentButtonType.SoloRed},
        {Gh5NeckInputType.TapYellow, InstrumentButtonType.SoloYellow},
        {Gh5NeckInputType.TapBlue, InstrumentButtonType.SoloBlue},
        {Gh5NeckInputType.TapOrange, InstrumentButtonType.SoloOrange}
    };


    public Gh5CombinedOutput(ConfigViewModel model, int sda = -1, int scl = -1, bool defaults = true) : base(model,
        "gh5", 100000, "GH5", sda, scl)
    {
        Outputs.Clear();
        if (defaults)
        {
            CreateDefaults();
        }
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    [Reactive] public bool Detected { get; set; }

    public void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "GH5 Slider Inputs";
    }

    public override object GetOutputType()
    {
        return SimpleType.Gh5NeckSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        Outputs.Add(new ControllerAxis(Model,
            new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, StandardAxisType.RightStickY, true));
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var tapAnalog =
            Outputs.Items.FirstOrDefault(
                s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(
                s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapAll}});
        if (tapAnalog == null && tapFrets == null) return Outputs.Items;
        var outputs = new List<Output>(Outputs.Items.Where(s => s.Enabled));

        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceType is DeviceControllerType.Guitar &&
            Model.RhythmType is RhythmType.RockBand)
        {
            outputs.AddRange(TapsRb.Select(pair => new GuitarButton(Model,
                new Gh5NeckInput(pair.Key, Model, Sda, Scl, true), Colors.Black, Colors.Black, Array.Empty<byte>(), 5,
                pair.Value, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;

        outputs.AddRange(Taps.Select(pair => new GuitarButton(Model, new Gh5NeckInput(pair.Key, Model, Sda, Scl, true),
            Colors.Black, Colors.Black, Array.Empty<byte>(), 5, pair.Value, true)));

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGh5CombinedOutput(Sda, Scl, Outputs.Items.ToList());
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
        Detected = gh5Raw.Any();
    }

    public override void UpdateBindings()
    {
        var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
        var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
        var tapAll = Outputs.Items.FirstOrDefault(s => s is OutputButton);
        if (Model.DeviceType is DeviceControllerType.Guitar)
        {
            if (tapAll == null)
            {
                var button = new GuitarButton(Model,
                    new Gh5NeckInput(Gh5NeckInputType.TapAll, Model, combined: true), Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5, InstrumentButtonType.Green, true);
                button.Enabled = false;
                Outputs.Add(button);
            }

            if (axisController == null) return;
            Outputs.Remove(axisController);
            Outputs.Add(new GuitarAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Sda, Scl,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                GuitarAxisType.Slider, true));
        }
        else
        {
            if (tapAll != null) Outputs.Remove(tapAll);

            if (axisGuitar == null) return;
            Outputs.Remove(axisGuitar);
            Outputs.Add(new ControllerAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Sda, Scl,
                    true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                StandardAxisType.LeftStickX, true));
        }
    }
}