using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;

public class Gh5NeckInput : IInput
{
    private static readonly Dictionary<int, BarButton> Mappings = new Dictionary<int, BarButton>()
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


    private static readonly List<Gh5NeckInputType> Frets = new List<Gh5NeckInputType>()
    {
        Gh5NeckInputType.Yellow,
        Gh5NeckInputType.Blue,
        Gh5NeckInputType.Red,
        Gh5NeckInputType.Green,
        Gh5NeckInputType.None,
        Gh5NeckInputType.None,
        Gh5NeckInputType.None,
        Gh5NeckInputType.Orange
    };

    private static readonly List<Gh5NeckInputType> Tap = new List<Gh5NeckInputType>()
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

    public Gh5NeckInput(Gh5NeckInputType input)
    {
        Input = input;
    }

    public Gh5NeckInputType Input { get; set; }
    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        if (Frets.Contains(this.Input))
        {
            return $"(fivetar_buttons[0] & {1 << Frets.IndexOf(this.Input)})";
        }

        if (this.Input == Gh5NeckInputType.TapBar)
        {
            return "fivetar_buttons[1]";
        }

        var mappings = MappingByInput[this.Input];
        return String.Join(" || ", mappings.Select(mapping => $"(fivetar_buttons[1] == {mapping})"));
    }

    public bool IsAnalog()
    {
        return Input == Gh5NeckInputType.TapBar;
    }

    public bool RequiresSpi()
    {
        return true;
    }

    public bool RequiresI2C()
    {
        return true;
    }

    public string GenerateAll(bool xbox, List<Tuple<IInput, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        return String.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        if (Input <= Gh5NeckInputType.Orange)
        {
            return new[] {"INPUT_GH5_NECK"};
        }
        else
        {
            return new[] {"INPUT_GH5_NECK", "INPUT_GH5_NECK_TAP_BAR"};
        }
    }
    
}