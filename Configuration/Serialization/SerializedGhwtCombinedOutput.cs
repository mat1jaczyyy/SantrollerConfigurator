using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhwtCombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Pin { get; }
    [ProtoMember(5)] public bool MapTapBarToFrets { get; set; }
    [ProtoMember(6)] public bool MapTapBarToAxis { get; set; }

    public SerializedGhwtCombinedOutput(Color ledOn, Color ledOff, int pin, bool mapTapBarToFrets, bool mapTapBarToAxis)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Pin = pin;
        MapTapBarToFrets = mapTapBarToFrets;
        MapTapBarToAxis = mapTapBarToAxis;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new GhwtCombinedOutput(model, microcontroller, Pin, MapTapBarToFrets, MapTapBarToAxis);
    }
}