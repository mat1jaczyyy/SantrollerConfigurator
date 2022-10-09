using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGh5CombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    [ProtoMember(6)] public bool MapTapBarToFrets { get; }
    [ProtoMember(7)] public bool MapTapBarToAxis { get; }
    [ProtoMember(8)] public bool FretsEnabled { get; }

    public SerializedGh5CombinedOutput(Color ledOn, Color ledOff, int sda, int scl, bool fretsEnabled, bool mapTapBarToFrets,
        bool mapTapBarToAxis)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Sda = sda;
        Scl = scl;
        FretsEnabled = fretsEnabled;
        MapTapBarToFrets = mapTapBarToFrets;
        MapTapBarToAxis = mapTapBarToAxis;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new Gh5CombinedOutput(model, microcontroller, Sda, Scl, FretsEnabled, MapTapBarToFrets, MapTapBarToAxis);
    }
}