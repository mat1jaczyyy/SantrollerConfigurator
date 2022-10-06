using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInput : SerializedInput
{
    [ProtoMember(1)] private int Pin { get; }

    [ProtoMember(2)] private GhWtInputType Type { get; }

    public SerializedGhWtInput(int pin, GhWtInputType type)
    {
        Pin = pin;
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new GhWtTapInput(Type, microcontroller, Pin);
    }
}