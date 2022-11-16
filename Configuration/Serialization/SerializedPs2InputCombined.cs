using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2InputCombined : SerializedInput
{
    [ProtoMember(6)] private Ps2InputType Type { get; }

    public SerializedPs2InputCombined(Ps2InputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new Ps2Input(Type, microcontroller, combined: true);
    }
}