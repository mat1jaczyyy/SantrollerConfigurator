using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;
public class GhWtTapInput : InputWithPin
{
    public GhWtInputType Input { get; set; }
    
    public override DevicePinMode PinMode => DevicePinMode.Floating;

    protected override Microcontroller Microcontroller => _microcontroller;
    public override int Pin { get; }

    public GhWtTapInput(GhWtInputType input, Microcontroller microcontroller, int pin = 0)
    {
        Input = input;
        _microcontroller = microcontroller;
        Pin = pin;
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

    private Microcontroller _microcontroller;

    public override string Generate()
    {
        if (Input == GhWtInputType.TapBar)
        {
            return "lastTap";
        }

        var mappings = MappingByInput[Input];
        return string.Join(" || ", mappings.Select(mapping => $"(lastTapShift == {mapping})"));
    }

    public override SerializedInput GetJson()
    {
        return new SerializedGhWtInput(Pin, Input);
    }

    public override bool IsAnalog => Input == GhWtInputType.TapBar;
    public override bool IsUint => true;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller)
    {
        return string.Join(";\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_WT_NECK", $"WT_NECK_READ() {_microcontroller.GeneratePulseRead(Pin, PulseMode.LOW, 100)}"};
    }
    
    public override List<DevicePin> Pins => new()
    {
        new (Pin, DevicePinMode.Floating),
    };
    

    public override void Dispose()
    {
    }
}