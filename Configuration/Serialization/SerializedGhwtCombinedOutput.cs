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
public class SerializedGhwtCombinedOutput : SerializedOutput
{
    public SerializedGhwtCombinedOutput(int pin, List<Output> outputs)
    {
        Pin = pin;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(1)] public SerializedInput? Input => null;
    [ProtoMember(4)] public int Pin { get; }

    [ProtoMember(5)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(6)] public byte[] Enabled { get; }

    public override Output Generate(ConfigViewModel model)
    {
        model.Microcontroller.AssignPin(new DirectPinConfig(model, GhWtTapInput.GhWtTapPinType, Pin, DevicePinMode.Floating));
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        return new GhwtCombinedOutput(model, Pin, outputs);
    }
}