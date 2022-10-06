using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDirectInput : SerializedInput
{
    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private DevicePinMode PinMode { get; }

    public SerializedDirectInput(int pin, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new DirectInput(Pin, PinMode, microcontroller);
    }
}