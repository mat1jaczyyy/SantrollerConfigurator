using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2Input : SerializedInput
{
    [ProtoMember(1)] private int Miso { get; }
    [ProtoMember(2)] private int Mosi { get; }
    [ProtoMember(3)] private int Sck { get; }
    [ProtoMember(4)] private int? Att { get; }
    [ProtoMember(5)] private int? Ack { get; }
    [ProtoMember(6)] private Ps2InputType Type { get; }

    public SerializedPs2Input(int miso, int mosi, int sck, int? att, int? ack, Ps2InputType type)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Att = att;
        Ack = ack;
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller)
    {
        return new Ps2Input(Type, microcontroller, Miso, Mosi, Sck, Att, Ack);
    }
}