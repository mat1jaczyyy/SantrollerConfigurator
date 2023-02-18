using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInputCombined : SerializedInput
{
    public SerializedGhWtInputCombined(GhWtInputType type)
    {
        Type = type;
    }

    [ProtoMember(2)] private GhWtInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, combined: true);
    }
}