using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedPs2CombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Miso { get; }
    [ProtoMember(5)] public int Mosi { get; }
    [ProtoMember(6)] public int Sck { get; }

    public SerializedPs2CombinedOutput(Color ledOn, Color ledOff, int miso, int mosi, int sck)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new Ps2CombinedOutput(model, microcontroller, Miso, Mosi, Sck);
    }
}