using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2Input : SerializedInput
{
    public SerializedPs2Input(int miso, int mosi, int sck, int? att, int? ack, Ps2InputType type)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Att = att;
        Ack = ack;
        Type = type;
    }

    [ProtoMember(1)] private int Miso { get; }
    [ProtoMember(2)] private int Mosi { get; }
    [ProtoMember(3)] private int Sck { get; }
    [ProtoMember(4)] private int? Att { get; }
    [ProtoMember(5)] private int? Ack { get; }
    [ProtoMember(6)] private Ps2InputType Type { get; }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new Ps2Input(Type, model, microcontroller, Miso, Mosi, Sck, Att, Ack);
    }
}