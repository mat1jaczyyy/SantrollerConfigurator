using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2InputCombined : SerializedInput
{
    public SerializedPs2InputCombined(Ps2InputType type)
    {
        Type = type;
    }

    [ProtoMember(6)] private Ps2InputType Type { get; }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new Ps2Input(Type, model, microcontroller, combined: true);
    }
}