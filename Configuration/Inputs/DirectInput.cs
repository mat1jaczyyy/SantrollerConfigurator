using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class DirectInput : InputWithPin
{
    public DirectInput(int pin, bool invert, DevicePinMode pinMode, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, pinMode))
    {
        Inverted = invert;
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    public override bool IsUint => true;

    [Reactive] public bool Inverted { get; set; }

    public override InputType? InputType => IsAnalog ? Types.InputType.AnalogPinInput : Types.InputType.DigitalPinInput;

    protected override string DetectionText => IsAnalog ? "Move the axis to detect" : "Press the button to detect";

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Pin, PinMode)
    };

    public override string Title => "Direct";

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        return Model.Microcontroller.Board.IsAvr()
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDirectInput(PinConfig.Pin, Inverted, PinConfig.PinMode);
    }

    public override string Generate()
    {
        return IsAnalog
            ? Model.Microcontroller.GenerateAnalogRead(PinConfig.Pin, Model)
            : Model.Microcontroller.GenerateDigitalRead(PinConfig.Pin, PinConfig.PinMode is DevicePinMode.PullUp);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }


    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType, byte[] usbHostInputsRaw, byte[] usbHostRaw)
    {
        if (IsAnalog)
        {
            RawValue = analogRaw.GetValueOrDefault(Pin, 0);
        }
        else
        {
            // Pullups mean low is a logical high, which is inherently an invert
            var invert = PinMode == DevicePinMode.PullUp;
            if (Inverted) invert = !invert;
            RawValue = digitalRaw.GetValueOrDefault(Pin, invert) switch
            {
                true when invert => 0,
                false when invert => 1,
                true => 1,
                false => 0
            };
        }
    }
}