using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiCombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    
    [ProtoMember(6)] public List<Output> Outputs { get; }
    public override Color LedOn => Colors.Transparent;
    public override Color LedOff => Colors.Transparent;
    public override int? LedIndex => null;

    public SerializedWiiCombinedOutput(int sda, int scl, List<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new WiiCombinedOutput(model, microcontroller, Sda, Scl, Outputs);
    }
}