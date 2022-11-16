using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInputCombined : SerializedInput
{
    [ProtoMember(2)] private GhWtInputType Type { get; }

    public SerializedGhWtInputCombined(GhWtInputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new GhWtTapInput(Type, microcontroller);
    }
}