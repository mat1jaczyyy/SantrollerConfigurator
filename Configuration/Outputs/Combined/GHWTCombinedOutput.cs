using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class GhwtCombinedOutput : CombinedOutput
{
    public int Pin { get; set; }
    private readonly Microcontroller _microcontroller;

    private static readonly Dictionary<GhWtInputType, StandardButtonType> Taps = new()
    {
        {GhWtInputType.TapGreen, StandardButtonType.A},
        {GhWtInputType.TapRed, StandardButtonType.B},
        {GhWtInputType.TapYellow, StandardButtonType.Y},
        {GhWtInputType.TapBlue, StandardButtonType.X},
        {GhWtInputType.TapOrange, StandardButtonType.LeftShoulder},
    };

    private static readonly Dictionary<GhWtInputType, RBButtonType> TapRb = new()
    {
        {GhWtInputType.TapGreen, RBButtonType.UpperGreen},
        {GhWtInputType.TapRed, RBButtonType.UpperRed},
        {GhWtInputType.TapYellow, RBButtonType.UpperYellow},
        {GhWtInputType.TapBlue, RBButtonType.UpperBlue},
        {GhWtInputType.TapOrange, RBButtonType.UpperOrange},
    };

    public GhwtCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? pin = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, null, "GHWT")
    {
        _microcontroller = microcontroller;
        if (pin.HasValue)
        {
            Pin = pin.Value;
        }

        Outputs.Clear();
        if (outputs != null)
        {
            Outputs.AddRange(outputs);
        }
        else
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

    public List<int> AvailablePins => _microcontroller.GetAllPins(false);

    public void CreateDefaults()
    {
        foreach (var pair in Taps)
        {
            Outputs.Add(new ControllerButton(Model,
                new GhWtTapInput(pair.Key, Model, _microcontroller,
                    combined: true), Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }

        UpdateBindings();
    }

    public void AddTapBarFrets()
    {
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGhwtCombinedOutput(Pin, Outputs.Items.ToList());
    }

    public override void UpdateBindings()
    {
        if (Model.DeviceType is DeviceControllerType.Guitar && Model.RhythmType == RhythmType.GuitarHero)
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis))
            {
                Outputs.Clear();
                Outputs.Add(new GuitarAxis(Model,
                    new GhWtTapInput(GhWtInputType.TapBar, Model, _microcontroller,
                        combined: true),
                    Colors.Transparent,
                    Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                    GuitarAxisType.Slider));
            }
        }
        else
        {
            if (!Outputs.Items.Any(s => s is ControllerAxis))
            {
                Outputs.Clear();
                Outputs.Add(new ControllerAxis(Model,
                    new GhWtTapInput(GhWtInputType.TapBar, Model, _microcontroller,
                        combined: true),
                    Colors.Transparent,
                    Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                    StandardAxisType.LeftStickX));
            }
        }

        // Map Tap bar to Upper frets on RB guitars, and standard frets on anything else
        if (Model.DeviceType is DeviceControllerType.Guitar && Model.RhythmType is RhythmType.RockBand)
        {
            var items = Outputs.Items.Where(s => s is ControllerButton).ToList();
            if (!items.Any()) return;
            Outputs.RemoveMany(items);
            Outputs.AddRange(items.Cast<RbButton>().Select(item => new RbButton(Model, item.Input,
                item.LedOn,
                item.LedOff, item.LedIndices.ToArray(), item.Debounce,
                TapRb[item.GhWtInputType])));
        }
        else
        {
            var items2 = Outputs.Items.Where(s => s is RbButton).ToList();
            if (!items2.Any()) return;
            Outputs.RemoveMany(items2);
            Outputs.AddRange(items2.Cast<RbButton>().Select(item => new ControllerButton(Model, item.Input,
                item.LedOn,
                item.LedOff, item.LedIndices.ToArray(), item.Debounce,
                Taps[item.GhWtInputType])));
        }
    }
}