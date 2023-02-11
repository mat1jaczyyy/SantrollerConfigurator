using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedConfiguration
{
    [ProtoMember(1)] public LedType LedType { get; }
    [ProtoMember(2)] public bool XInputOnWindows { get; }
    [ProtoMember(3)] public bool CombinedDebounce { get; }
    [ProtoMember(4)] public DeviceControllerType DeviceType { get; }
    [ProtoMember(5)] public EmulationType EmulationType { get; }
    [ProtoMember(6)] public RhythmType RhythmType { get; }
    [ProtoMember(7)] public List<SerializedOutput> Bindings { get; }
    [ProtoMember(8)] public int Apa102Mosi { get; }
    [ProtoMember(9)] public int Apa102Sck { get; }
    [ProtoMember(10)] public byte LedCount { get; }
    [ProtoMember(11)] public MouseMovementType MouseMovementType { get; }
    
    public SerializedConfiguration(ConfigViewModel model)
    {
        Bindings = model.Bindings.Select(s => s.Serialize()).ToList();
        LedType = model.LedType;
        DeviceType = model.DeviceType;
        EmulationType = model.EmulationType;
        RhythmType = model.RhythmType;
        XInputOnWindows = model.XInputOnWindows;
        CombinedDebounce = model.CombinedDebounce;
        Apa102Mosi = model.Apa102Mosi;
        Apa102Sck = model.Apa102Sck;
        LedCount = model.LedCount;
        MouseMovementType = model.MouseMovementType;
    }

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceType, RhythmType, EmulationType);
        model.CombinedDebounce = CombinedDebounce;
        model.XInputOnWindows = XInputOnWindows;
        model.MicroController!.UnAssignAll();
        model.Bindings.Clear();
        model.Bindings.AddRange(Bindings.Select(s => s.Generate(model, model.MicroController!)));
        model.LedType = LedType;
        model.LedCount = LedCount < 1 ? (byte) 1 : LedCount;
        model.MouseMovementType = MouseMovementType;
        if (!model.IsApa102) return;
        model.Apa102Mosi = Apa102Mosi;
        model.Apa102Sck = Apa102Sck;
    }
}