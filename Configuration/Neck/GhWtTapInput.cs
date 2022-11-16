using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;

public class GhWtTapInput : InputWithPin
{
    public GhWtInputType Input { get; set; }


    public GhWtTapInput(GhWtInputType input, Microcontroller microcontroller, int pin = 0) : base(microcontroller,
        new DirectPinConfig(Guid.NewGuid().ToString(), pin, DevicePinMode.Floating))
    {
        Input = input;
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
        return new SerializedGhWtInput(PinConfig.Pin, Input);
    }

    public override bool IsAnalog => Input == GhWtInputType.TapBar;
    public override InputType? InputType => Types.InputType.WtNeckInput;
    public override bool IsUint => true;


    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        if (!ghWtRaw.Any()) return;
        RawValue = BitConverter.ToInt32(ghWtRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings)
    {
        return string.Join(";\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[]
            {"INPUT_WT_NECK", $"WT_NECK_READ() {Microcontroller.GeneratePulseRead(PinConfig.Pin, PulseMode.LOW, 100)}"};
    }

    public override List<DevicePin> Pins => new()
    {
        new(PinConfig.Pin, DevicePinMode.Floating),
    };
}