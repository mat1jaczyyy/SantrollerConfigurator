using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiInputCombined : SerializedInput
{
    [ProtoMember(3)] private WiiInputType Type { get; }

    public SerializedWiiInputCombined(WiiInputType type)
    {
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new WiiInput(Type, model,microcontroller, combined:true);
    }
}