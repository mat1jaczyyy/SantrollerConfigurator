using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDigitalToAnalog : SerializedInput
{
    [ProtoMember(1)] private SerializedInput Child { get; }
    [ProtoMember(2)] private int On { get; }
    [ProtoMember(3)] private int Off { get; }

    public SerializedDigitalToAnalog(SerializedInput child, int on, int off)
    {
        Child = child;
        On = on;
        Off = off;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new DigitalToAnalog(Child.Generate(microcontroller, model), On, Off, model);
    }
}