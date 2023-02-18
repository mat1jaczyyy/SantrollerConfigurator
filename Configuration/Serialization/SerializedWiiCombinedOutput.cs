using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiCombinedOutput : SerializedOutput
{
    public SerializedWiiCombinedOutput(int sda, int scl, List<Output> outputs)
    {
        Sda = sda;
        Scl = scl;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(7)] public byte[] Enabled { get; }
    public override uint LedOn => Colors.Black.ToUint32();
    public override uint LedOff => Colors.Black.ToUint32();
    public override byte[] LedIndex => Array.Empty<byte>();

    public override Output Generate(ConfigViewModel model)
    {
        // Since we filter out sda and scl from wii inputs for size, we need to make sure its assigned before we construct the inputs.
        model.Microcontroller.AssignTwiPins(model, WiiInput.WiiTwiType, Sda, Scl, WiiInput.WiiTwiFreq);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        var array = new BitArray(Enabled);
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];

        return new WiiCombinedOutput(model, Sda, Scl, outputs);
    }
}