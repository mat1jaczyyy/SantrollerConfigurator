using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDirectInput : SerializedInput
{
    public SerializedDirectInput(int pin, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
    }

    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private DevicePinMode PinMode { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new DirectInput(Pin, PinMode, model);
    }
}