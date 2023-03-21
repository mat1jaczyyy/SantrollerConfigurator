using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGh5NeckInputCombined : SerializedInput
{
    public SerializedGh5NeckInputCombined(Gh5NeckInputType type)
    {
        Type = type;
    }

    [ProtoMember(3)] private Gh5NeckInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new Gh5NeckInput(Type, model, combined: true);
    }
}