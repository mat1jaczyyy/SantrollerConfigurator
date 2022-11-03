using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2CombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(4)] public int Miso { get; }
    [ProtoMember(5)] public int Mosi { get; }
    [ProtoMember(6)] public int Sck { get; }
    [ProtoMember(7)] public int Att { get; }
    [ProtoMember(8)] public int Ack { get; }
    public override Color LedOn => Colors.Transparent;
    public override Color LedOff => Colors.Transparent;
    public override int? LedIndex => null;

    public SerializedPs2CombinedOutput(int miso, int mosi, int sck, int att, int ack)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Att = att;
        Ack = ack;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new Ps2CombinedOutput(model, microcontroller, Miso, Mosi, Sck, Att, Ack);
    }
}