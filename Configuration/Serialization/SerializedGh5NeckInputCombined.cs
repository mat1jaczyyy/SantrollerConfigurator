using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGh5NeckInputCombined : SerializedInput
{
    [ProtoMember(3)] private Gh5NeckInputType Type { get; }

    public SerializedGh5NeckInputCombined(Gh5NeckInputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new Gh5NeckInput(Type, model, microcontroller, combined: true);
    }
}