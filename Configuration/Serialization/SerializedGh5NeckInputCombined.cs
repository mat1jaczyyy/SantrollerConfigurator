using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGh5NeckInputCombined : SerializedInput
{
    [ProtoMember(3)] private Gh5NeckInputType Type { get; }

    public SerializedGh5NeckInputCombined(Gh5NeckInputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new Gh5NeckInput(Type, microcontroller);
    }
}