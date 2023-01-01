using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Neck;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInputCombined : SerializedInput
{
    [ProtoMember(2)] private GhWtInputType Type { get; }

    public SerializedGhWtInputCombined(GhWtInputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, microcontroller, combined: true);
    }
}