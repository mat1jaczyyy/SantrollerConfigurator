using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiInput : SerializedInput
{
    public SerializedWiiInput(int sda, int scl, WiiInputType type)
    {
        Sda = sda;
        Scl = scl;
        Type = type;
    }

    [ProtoMember(1)] private int Sda { get; }
    [ProtoMember(2)] private int Scl { get; }
    [ProtoMember(3)] private WiiInputType Type { get; }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new WiiInput(Type, model, microcontroller, Sda, Scl);
    }
}