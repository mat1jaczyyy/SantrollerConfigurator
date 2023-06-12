using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedConfiguration
{
    public SerializedConfiguration(ConfigViewModel model)
    {
        Bindings = model.Bindings.Items.Select(s => s.Serialize()).ToList();
        LedType = model.LedType;
        DeviceType = model.DeviceType;
        EmulationType = model.EmulationType;
        RhythmType = model.RhythmType;
        XInputOnWindows = model.XInputOnWindows;
        Apa102Mosi = model.Apa102Mosi;
        Apa102Sck = model.Apa102Sck;
        LedCount = model.LedCount;
        MouseMovementType = model.MouseMovementType;
        WtSensitivity = model.WtSensitivity;
        UsbHostDp = model.UsbHostDp;
        PowerLevel = model.PowerLevel;
        RfMiso = model.RfMiso;
        RfMosi = model.RfMosi;
        RfSck = model.RfSck;
        RfCe = model.RfCe;
        RfCsn = model.RfCsn;
        RfChannel = model.RfChannel;
        RfDeviceId = model.RfId;
        Mode = model.Mode;
        Debounce = model.Debounce;
        StrumDebounce = model.StrumDebounce;
        PollRate = model.PollRate;
        CombinedStrumDebounce = model.CombinedStrumDebounce;
        QueueBasedInputs = model.Deque;
    }

    [ProtoMember(1)] public LedType LedType { get; }
    [ProtoMember(2)] public bool XInputOnWindows { get; }
    [ProtoMember(4)] public DeviceControllerType DeviceType { get; }
    [ProtoMember(5)] public EmulationType EmulationType { get; }
    [ProtoMember(6)] public RhythmType RhythmType { get; }
    [ProtoMember(7)] public List<SerializedOutput>? Bindings { get; }
    [ProtoMember(8)] public int Apa102Mosi { get; }
    [ProtoMember(9)] public int Apa102Sck { get; }
    [ProtoMember(10)] public byte LedCount { get; }
    [ProtoMember(11)] public MouseMovementType MouseMovementType { get; }
    [ProtoMember(12)] public byte WtSensitivity { get; }
    [ProtoMember(14)] public int UsbHostDp { get; }
    [ProtoMember(15)] public RfPowerLevel PowerLevel { get; }
    [ProtoMember(16)] public int RfMosi { get; }
    [ProtoMember(17)] public int RfMiso { get; }
    [ProtoMember(18)] public int RfSck { get; }
    [ProtoMember(19)] public int RfCe { get; }
    [ProtoMember(20)] public int RfCsn { get; }
    [ProtoMember(21)] public byte RfChannel { get; }
    [ProtoMember(22)] public byte RfDeviceId { get; }
    [ProtoMember(23)] public ModeType Mode { get; }
    [ProtoMember(24)] public int Debounce { get; }
    [ProtoMember(25)] public int StrumDebounce { get; }
    [ProtoMember(26)] public int PollRate { get; }
    [ProtoMember(27)] public bool CombinedStrumDebounce { get; }
    [ProtoMember(28)] public bool QueueBasedInputs { get; }

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceType, RhythmType, EmulationType);
        model.XInputOnWindows = XInputOnWindows;
        model.Bindings.Clear();
        model.Mode = Mode;
        model.PollRate = PollRate;
        model.Debounce = Debounce;
        model.StrumDebounce = StrumDebounce;
        model.Deque = QueueBasedInputs;
        if (Bindings != null)
        {
            var generated = Bindings.Select(s => s.Generate(model)).ToList();
            model.Bindings.Clear();
            model.Bindings.AddRange(generated);
        }
        if (model.UsbHostEnabled) model.UsbHostDp = UsbHostDp;

        model.LedType = LedType;
        model.LedCount = LedCount < 1 ? (byte) 1 : LedCount;
        model.WtSensitivity = WtSensitivity;
        model.MouseMovementType = MouseMovementType;
        model.CombinedStrumDebounce = CombinedStrumDebounce;

        if (model.IsRf)
        {
            model.PowerLevel = PowerLevel;
            model.RfChannel = RfChannel;
            model.RfId = RfDeviceId;
            model.RfMiso = RfMiso;
            model.RfMosi = RfMosi;
            model.RfSck = RfSck;
            model.RfCe = RfCe;
            model.RfCsn = RfCsn;
        }

        if (!model.IsApa102) return;
        model.Apa102Mosi = Apa102Mosi;
        model.Apa102Sck = Apa102Sck;
    }
}