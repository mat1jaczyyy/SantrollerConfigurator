using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
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
    
    [ProtoMember(9)] public List<SerializedOutput> Outputs { get; }
    public override uint LedOn => Colors.Transparent.ToUint32();
    public override uint LedOff => Colors.Transparent.ToUint32();
    public override byte LedIndex => 0;

    public SerializedPs2CombinedOutput(int miso, int mosi, int sck, int att, int ack, List<Output> outputs)
    {
        Miso = miso;
        Mosi = mosi;
        Sck = sck;
        Att = att;
        Ack = ack;
        Outputs = outputs.Select(s => s.Serialize()).ToList();
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        // Since we filter out sda and scl from wii inputs for size, we need to make sure its assigned before we construct the inputs.
        microcontroller.AssignSpiPins(Ps2Input.Ps2SpiType, Mosi, Miso, Sck, Ps2Input.Ps2SpiCpol, Ps2Input.Ps2SpiCpha, Ps2Input.Ps2SpiMsbFirst, Ps2Input.Ps2SpiFreq);
        microcontroller.AssignPin(new DirectPinConfig(Ps2Input.Ps2AckType, Ack, DevicePinMode.Floating));
        microcontroller.AssignPin(new DirectPinConfig(Ps2Input.Ps2AttType, Att, DevicePinMode.Output));
        return new Ps2CombinedOutput(model, microcontroller, Miso, Mosi, Sck, Att, Ack, Outputs.Select(s => s.Generate(model, microcontroller)).ToList());
    }
}