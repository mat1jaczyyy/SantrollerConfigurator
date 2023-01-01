using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;

public class Gh5NeckInput : TwiInput
{
    public static readonly string Gh5TwiType = "gh5";
    public static readonly int Gh5TwiFreq = 100000;

    private static readonly Dictionary<int, BarButton> Mappings = new()
    {
        {0x19, BarButton.Green | BarButton.Yellow},
        {0x1A, BarButton.Yellow},
        {0x2C, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0x2D, BarButton.Green | BarButton.Yellow | BarButton.Blue},
        {0x2E, BarButton.Red | BarButton.Yellow | BarButton.Blue},
        {0x2F, BarButton.Yellow | BarButton.Blue},
        {0x46, BarButton.Green | BarButton.Red | BarButton.Blue},
        {0x47, BarButton.Green | BarButton.Blue},
        {0x48, BarButton.Red | BarButton.Blue},
        {0x49, BarButton.Blue},
        {0x5F, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x60, BarButton.Green | BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0x61, BarButton.Green | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x62, BarButton.Green | BarButton.Blue | BarButton.Orange},
        {0x63, BarButton.Red | BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x64, BarButton.Red | BarButton.Blue | BarButton.Orange},
        {0x65, BarButton.Yellow | BarButton.Blue | BarButton.Orange},
        {0x66, BarButton.Blue | BarButton.Orange},
        {0x78, BarButton.Green | BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0x79, BarButton.Green | BarButton.Red | BarButton.Orange},
        {0x7A, BarButton.Green | BarButton.Yellow | BarButton.Orange},
        {0x7B, BarButton.Green | BarButton.Orange},
        {0x7C, BarButton.Red | BarButton.Yellow | BarButton.Orange},
        {0x7D, BarButton.Red | BarButton.Orange},
        {0x7E, BarButton.Yellow | BarButton.Orange},
        {0x7F, BarButton.Orange},
        {0x95, BarButton.Green},
        {0xB0, BarButton.Green | BarButton.Red},
        {0xCD, BarButton.Red},
        {0xE5, BarButton.Green | BarButton.Red | BarButton.Yellow},
        {0xE6, BarButton.Red | BarButton.Yellow},
    };

    private static readonly List<Gh5NeckInputType> Tap = new()
    {
        Gh5NeckInputType.TapGreen,
        Gh5NeckInputType.TapRed,
        Gh5NeckInputType.TapYellow,
        Gh5NeckInputType.TapBlue,
        Gh5NeckInputType.TapOrange
    };

    private static readonly Dictionary<Gh5NeckInputType, BarButton> InputToButton = new()
    {
        {Gh5NeckInputType.TapGreen, BarButton.Green},
        {Gh5NeckInputType.TapRed, BarButton.Red},
        {Gh5NeckInputType.TapYellow, BarButton.Yellow},
        {Gh5NeckInputType.TapBlue, BarButton.Blue},
        {Gh5NeckInputType.TapOrange, BarButton.Orange},
    };

    private static readonly Dictionary<Gh5NeckInputType, ReadOnlyCollection<int>> MappingByInput =
        Tap.ToDictionary(type => type,
            type => Mappings.Where(mapping => mapping.Value.HasFlag((InputToButton[type])))
                .Select(mapping => mapping.Key).ToList().AsReadOnly());

    public bool Combined { get; }
    
    public bool BindableTwi { get; }

    public Gh5NeckInput(Gh5NeckInputType input, ConfigViewModel model, Microcontroller controller, int? sda = null, int? scl = null, bool combined = false) : base(
        controller,
        Gh5TwiType, Gh5TwiFreq, sda, scl, model)
    {
        Combined = combined;
        BindableTwi = !combined && controller is not AvrController;
        Input = input;
        IsAnalog = Input == Gh5NeckInputType.TapBar;
    }

    public override InputType? InputType => Types.InputType.Gh5NeckInput;
    public Gh5NeckInputType Input { get; set; }

    public override string Generate(bool xbox)
    {
        if (Input <= Gh5NeckInputType.Orange)
        {
            return $"(fivetar_buttons[0] & {1 << ((byte) Input) - ((byte) Gh5NeckInputType.Green)})";
        }

        if (Input == Gh5NeckInputType.TapBar)
        {
            return "fivetar_buttons[1]";
        }

        var mappings = MappingByInput[Input];
        return "if (gh5Valid) {" +
               string.Join(" || ", mappings.Select(mapping => $"(fivetar_buttons[1] == {mapping})")) + "}";
    }

    public override SerializedInput Serialise()
    {
        if (Combined)
        {
            return new SerializedGh5NeckInputCombined(Input);
        }
        return new SerializedGh5NeckInput(Sda, Scl, Input);
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        if (!gh5Raw.Any()) return;
        switch (Input)
        {
            case <= Gh5NeckInputType.Orange:
                RawValue = (gh5Raw[0] & (1 << ((byte) Input) - ((byte) Gh5NeckInputType.Green))) != 0 ? 1 : 0;
                break;
            case Gh5NeckInputType.TapBar:
                RawValue = gh5Raw[1];
                break;
            default:
            {
                var mappings = MappingByInput[Input];
                RawValue = mappings.Contains(gh5Raw[1]) ? 1 : 0;
                break;
            }
        }
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings, bool shared,
        bool xbox)
    {
        return string.Join(";\n", bindings.Select(binding => binding.Item2));
    }

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override bool IsUint => true;


    public override IReadOnlyList<string> RequiredDefines()
    {
        if (Input <= Gh5NeckInputType.Orange)
        {
            return base.RequiredDefines().Concat(new[] {"INPUT_GH5_NECK"}).ToList();
        }

        return base.RequiredDefines().Concat(new[] {"INPUT_GH5_NECK", "INPUT_GH5_NECK_TAP_BAR"}).ToList();
    }
}