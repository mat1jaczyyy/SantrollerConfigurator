using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGh5CombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }

    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    public override uint LedOn => Colors.Transparent.ToUint32();
    public override uint LedOff => Colors.Transparent.ToUint32();
    public override byte LedIndex => 0;

    public SerializedGh5CombinedOutput(int sda, int scl, List<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        // Since we filter out sda and scl from wii inputs for size, we need to make sure its assigned before we construct the inputs.
        microcontroller.AssignTwiPins(Gh5NeckInput.Gh5TwiType, Sda, Scl, Gh5NeckInput.Gh5TwiFreq);
        return new Gh5CombinedOutput(model, microcontroller, Sda, Scl,
            Outputs.Select(s => s.Generate(model, microcontroller)).ToList());
    }
}