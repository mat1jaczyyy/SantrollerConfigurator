using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjInputCombined : SerializedInput
{
    public SerializedDjInputCombined(DjInputType type)
    {
        Type = type;
    }

    [ProtoMember(3)] private DjInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new DjInput(Type, model, true, combined: true);
    }
}