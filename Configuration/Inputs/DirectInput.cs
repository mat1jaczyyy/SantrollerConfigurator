using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class DirectInput : InputWithPin
{
    public DirectInput(int pin, DevicePinMode pinMode, ConfigViewModel model) : base(
        model, new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, pinMode))
    {
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    public override bool IsUint => true;

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
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.PinMode);
    }

    public override string Generate(ConfigField mode)
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
        byte[] wiiControllerType)
    {
        if (IsAnalog)
            RawValue = analogRaw.GetValueOrDefault(Pin, 0);
        else
            RawValue = (digitalRaw.GetValueOrDefault(Pin, true) ? PinMode == DevicePinMode.PullUp : PinMode == DevicePinMode.PullDown) ? 1 : 0;
    }
}