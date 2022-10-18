using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjCombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }

    public SerializedDjCombinedOutput(Color ledOn, Color ledOff, int sda, int scl)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Sda = sda;
        Scl = scl;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new DjCombinedOutput(model, microcontroller, Sda, Scl);
    }
}