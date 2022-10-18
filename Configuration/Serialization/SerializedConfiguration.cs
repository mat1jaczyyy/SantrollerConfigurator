using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

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
    [ProtoMember(8)] public LedOrderType LedOrder { get; }
    
    public SerializedConfiguration(ConfigViewModel model)
    {
        Bindings = model.Bindings.Select(s => s.GetJson()).ToList();
        LedType = model.LedType;
        DeviceType = model.DeviceType;
        EmulationType = model.EmulationType;
        RhythmType = model.RhythmType;
        XInputOnWindows = model.XInputOnWindows;
        CombinedDebounce = model.CombinedDebounce;
        LedOrder = model.LedOrder;
    }

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.CombinedDebounce = CombinedDebounce;
        model.DeviceType = DeviceType;
        model.EmulationType = EmulationType;
        model.RhythmType = RhythmType;
        model.LedType = LedType;
        model.XInputOnWindows = XInputOnWindows;
        model.Bindings.Clear();
        model.Bindings.AddRange(Bindings.Select(s => s.Generate(model, model.MicroController!)));
        model.LedOrder = LedOrder;
    }
}