using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
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

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new DjInput(Type, model, microcontroller, combined: true);
    }
}