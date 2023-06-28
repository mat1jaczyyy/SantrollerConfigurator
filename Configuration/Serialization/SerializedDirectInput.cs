using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDirectInput : SerializedInput
{
    public SerializedDirectInput(int pin, bool inverted, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
        Inverted = inverted;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private DevicePinMode PinMode { get; }
    [ProtoMember(3)] private bool Inverted { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new DirectInput(Pin, Inverted, PinMode, model);
    }
}