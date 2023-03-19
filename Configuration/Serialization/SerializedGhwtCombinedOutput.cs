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
    public SerializedGhwtCombinedOutput(int pin, int pinS0, int pinS1, int pinS2, List<Output> outputs)
    {
        Pin = pin;
        PinS0 = pinS0;
        PinS1 = pinS1;
        PinS2 = pinS2;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
        Enabled = GetBytes(new BitArray(outputs.Select(s => s.Enabled).ToArray()));
    }

    [ProtoMember(1)] public SerializedInput? Input => null;
    [ProtoMember(2)] public int Pin { get; }
    [ProtoMember(3)] public int PinS0 { get; }

    [ProtoMember(4)] public int PinS1 { get; }

    [ProtoMember(5)] public int PinS2 { get; }


    [ProtoMember(6)] public List<SerializedOutput> Outputs { get; }
    [ProtoMember(7)] public byte[] Enabled { get; }

    public override Output Generate(ConfigViewModel model)
    {
        model.Microcontroller.AssignPin(new DirectPinConfig(model, GhWtTapInput.GhWtAnalogPinType, Pin, DevicePinMode.Floating));
        model.Microcontroller.AssignPin(new DirectPinConfig(model, GhWtTapInput.GhWtS0PinType, PinS0, DevicePinMode.Floating));
        model.Microcontroller.AssignPin(new DirectPinConfig(model, GhWtTapInput.GhWtS1PinType, PinS1, DevicePinMode.Floating));
        model.Microcontroller.AssignPin(new DirectPinConfig(model, GhWtTapInput.GhWtS2PinType, PinS2, DevicePinMode.Floating));
        var array = new BitArray(Enabled);
        var outputs = Outputs.Select(s => s.Generate(model)).ToList();
        for (var i = 0; i < outputs.Count; i++) outputs[i].Enabled = array[i];
        return new GhwtCombinedOutput(model, Pin, PinS0, PinS1, PinS2, outputs);
    }
}