using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;

public class GhWtTapInput : InputWithPin
{
    public GhWtInputType Input { get; set; }
    
    public override DevicePinMode PinMode => DevicePinMode.Analog;

    protected override Microcontroller.Microcontroller Microcontroller => _microcontroller;

    // TODO this should probably directly link to and control something in ConfigViewModel as it is global and needs to be shared.
    public override int Pin { get; }

    public GhWtTapInput(GhWtInputType input, Microcontroller.Microcontroller microcontroller)
    {
        Input = input;
        _microcontroller = microcontroller;
    }

    static readonly Dictionary<int, BarButton> Mappings = new()
    {
        {0x17, BarButton.Green},
        {0x16, BarButton.Green},
        {0x14, BarButton.Green | BarButton.Red},
        {0x11, BarButton.Red},
        {0x12, BarButton.Red},
        {0xf, BarButton.Red | BarButton.Yellow},
        {0xa, BarButton.Yellow},
        {0xb, BarButton.Yellow},
        {0x9, BarButton.Yellow | BarButton.Blue},
        {0x7, BarButton.Blue},
        {0x5, BarButton.Blue | BarButton.Orange},
        {0x4, BarButton.Blue | BarButton.Orange},
        {0x3, BarButton.Blue | BarButton.Orange},
        {0x0, BarButton.Orange},
    };
    
    private static readonly Dictionary<GhWtInputType, BarButton> InputToButton = new()
    {
        {GhWtInputType.TapGreen, BarButton.Green},
        {GhWtInputType.TapRed, BarButton.Red},
        {GhWtInputType.TapYellow, BarButton.Yellow},
        {GhWtInputType.TapBlue, BarButton.Blue},
        {GhWtInputType.TapOrange, BarButton.Orange},
    };
    
    private static readonly Dictionary<GhWtInputType, ReadOnlyCollection<int>> MappingByInput =
        InputToButton.Keys.ToDictionary(type => type,
            type => Mappings.Where(mapping => mapping.Value.HasFlag((InputToButton[type])))
                .Select(mapping => mapping.Key).ToList().AsReadOnly());

    private Microcontroller.Microcontroller _microcontroller;

    public override string Generate()
    {
        if (Input == GhWtInputType.TapBar)
        {
            return "lastTap";
        }

        var mappings = MappingByInput[Input];
        return String.Join(" || ", mappings.Select(mapping => $"(lastTap == {mapping})"));
    }

    public override bool IsAnalog => Input == GhWtInputType.TapBar;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        return String.Join(";\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_WT_NECK"};
    }
}

// Note that we would need to generate the right pulse reading command
// long pulse = readPulse;
// if (pulse == readPulse) {
//     lastTapShifted = pulse << 1;
//     lastTap = pulse;
// }