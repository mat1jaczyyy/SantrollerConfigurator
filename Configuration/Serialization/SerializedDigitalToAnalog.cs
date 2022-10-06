using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDigitalToAnalog : SerializedInput
{
    [ProtoMember(1)] private SerializedInput Child { get; }
    [ProtoMember(2)] private int Value { get; }

    public SerializedDigitalToAnalog(SerializedInput child, int value)
    {
        Child = child;
        Value = value;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new DigitalToAnalog(Child.Generate(microcontroller), Value);
    }
}