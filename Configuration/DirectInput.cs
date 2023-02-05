using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration;

public class DirectInput : InputWithPin
{
    public DirectInput(int pin, DevicePinMode pinMode, ConfigViewModel model, Microcontroller microcontroller) : base(
        model, microcontroller,
        new DirectPinConfig(model, Guid.NewGuid().ToString(), pin, pinMode))
    {
        IsAnalog = PinConfig.PinMode == DevicePinMode.Analog;
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues(typeof(DevicePinMode)).Cast<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        return Microcontroller.Board.IsAvr()
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }


    public override SerializedInput Serialise()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.PinMode);
    }

    public override bool IsUint => true;
    public override string Generate(DeviceEmulationMode mode)
    {
        return IsAnalog
            ? Microcontroller.GenerateAnalogRead(PinConfig.Pin)
            : Microcontroller.GenerateDigitalRead(PinConfig.Pin, PinConfig.PinMode is DevicePinMode.PullUp);
    }

    public override InputType? InputType => IsAnalog ? Types.InputType.AnalogPinInput : Types.InputType.DigitalPinInput;

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings, 
        DeviceEmulationMode mode)
    {
        if (Microcontroller is not AvrController) return string.Join(";\n", bindings.Select(binding => binding.Item2));
        var replacements = new Dictionary<string, string>();
        var seenPins = allBindings.Select(s => s.Input?.InnermostInput()).OfType<DirectInput>().Where(s => s.IsAnalog)
            .Select(s => s.Pin).Distinct().OrderBy(s => s).Select((pin, index) => (pin, index)).ToDictionary(s => s.pin, s => s.index);
        foreach (var (item1, item2) in bindings)
        {
            var pin = item1.Pins.First().Pin;
            if (item1.IsAnalog)
            {
                replacements[item2] = item2.Replace("{pin}", seenPins[pin].ToString());
            }
        }

        return string.Join(";\n", bindings.Select(b => b.Item1.IsAnalog ? replacements[b.Item2] : b.Item2));
    }

    public override void Dispose()
    {
    }

    protected override string DetectionText => IsAnalog ? "Move the axis to detect" : "Press the button to detect";

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }

    public override IList<DevicePin> Pins => new List<DevicePin>()
    {
        new(Pin, PinMode)
    };

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType)
    {
        if (IsAnalog)
        {
            RawValue =  analogRaw.GetValueOrDefault(Pin, 0);
        }
        else
        {
            RawValue = digitalRaw.GetValueOrDefault(Pin, true) ? 0 : 1;
        }
    }
}