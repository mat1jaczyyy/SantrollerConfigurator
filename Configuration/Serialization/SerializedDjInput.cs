using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjInput : SerializedInput
{
    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private DjInputType Type { get; }

    public SerializedDjInput(int sda, int scl, DjInputType type)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new DjInput(Type, model, microcontroller, Sda, Scl);
    }
}