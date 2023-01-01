using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

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