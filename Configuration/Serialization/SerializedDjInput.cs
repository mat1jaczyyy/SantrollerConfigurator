using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjInput : SerializedInput
{
    public SerializedDjInput(int sda, int scl, DjInputType type)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private DjInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new DjInput(Type, model, Sda, Scl);
    }
}