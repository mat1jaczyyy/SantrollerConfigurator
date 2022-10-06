using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
[ProtoInclude(101, typeof(SerializedAnalogToDigital))]
[ProtoInclude(102, typeof(SerializedDigitalToAnalog))]
[ProtoInclude(103, typeof(SerializedDirectInput))]
[ProtoInclude(104, typeof(SerializedDjInput))]
[ProtoInclude(105, typeof(SerializedGh5NeckInput))]
[ProtoInclude(106, typeof(SerializedGhWtInput))]
[ProtoInclude(107, typeof(SerializedPs2Input))]
[ProtoInclude(108, typeof(SerializedWiiInput))]
public abstract class SerializedInput
{
    public abstract Input Generate(Microcontroller microcontroller);
}