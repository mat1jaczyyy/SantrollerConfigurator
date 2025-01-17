using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedConfiguration
{
    public SerializedConfiguration(ConfigViewModel model)
    {
        Update(model, model.Bindings.Items);
    }

    public void Update(ConfigViewModel model, IEnumerable<Output> bindings, bool allowErrors = true)
    {
        if (!allowErrors && model.HasError) return;
        Bindings = bindings.Select(s => s.Serialize()).ToList();
        LedType = model.LedType;
        DeviceType = model.DeviceControllerType;
        EmulationType = model.EmulationType;
        XInputOnWindows = model.XInputOnWindows;
        Apa102Mosi = model.Apa102Mosi;
        Apa102Sck = model.Apa102Sck;
        LedCount = model.LedCount;
        MouseMovementType = model.MouseMovementType;
        WtSensitivity = model.WtSensitivity;
        UsbHostDp = model.UsbHostDp;
        Mode = model.Mode;
        Debounce = model.Debounce;
        StrumDebounce = model.StrumDebounce;
        PollRate = model.PollRate;
        CombinedStrumDebounce = model.CombinedStrumDebounce;
        QueueBasedInputs = model.Deque;
        DjPollRate = model.DjPollRate;
        DjDual = model.DjDual;
        DjSmooth = model.DjSmoothing;
        SwapSwitchFaceButtons = model.SwapSwitchFaceButtons;
        Variant = model.Variant;
    }

    [ProtoMember(1)] public LedType LedType { get; private set; }
    [ProtoMember(2)] public bool XInputOnWindows { get; private set; }
    [ProtoMember(4)] public DeviceControllerType DeviceType { get; private set; }
    [ProtoMember(5)] public EmulationType EmulationType { get; private set; }
    [ProtoMember(7)] public List<SerializedOutput>? Bindings { get; private set; }
    [ProtoMember(8)] public int Apa102Mosi { get; private set; }
    [ProtoMember(9)] public int Apa102Sck { get; private set; }
    [ProtoMember(10)] public byte LedCount { get; private set; }
    [ProtoMember(11)] public MouseMovementType MouseMovementType { get; private set; }
    [ProtoMember(12)] public byte WtSensitivity { get; private set; }
    [ProtoMember(14)] public int UsbHostDp { get; private set; }
    [ProtoMember(23)] public ModeType Mode { get; private set; }
    [ProtoMember(24)] public int Debounce { get; private set; }
    [ProtoMember(25)] public int StrumDebounce { get; private set; }
    [ProtoMember(26)] public int PollRate { get; private set; }
    [ProtoMember(27)] public bool CombinedStrumDebounce { get; private set; }
    [ProtoMember(28)] public bool QueueBasedInputs { get; private set; }
    [ProtoMember(29)] public int DjPollRate { get; private set; }
    [ProtoMember(30)] public bool DjDual { get; private set; }
    [ProtoMember(31)] public bool SwapSwitchFaceButtons { get; private set; }
    [ProtoMember(32)] public bool DjSmooth { get; private set; }
    [ProtoMember(33)] public string Variant { get; private set; } = "";

    public void LoadConfiguration(ConfigViewModel model)
    {
        model.SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceType, EmulationType);
        model.XInputOnWindows = XInputOnWindows;
        model.Bindings.Clear();
        model.Mode = Mode;
        model.PollRate = PollRate;
        model.Debounce = Debounce;
        model.StrumDebounce = StrumDebounce;
        model.Deque = QueueBasedInputs;
        model.DjPollRate = DjPollRate;
        model.DjDual = DjDual;
        model.DjSmoothing = DjSmooth;
        model.SwapSwitchFaceButtons = SwapSwitchFaceButtons;
        model.Variant = Variant;
        if (DjPollRate == 0)
        {
            model.DjPollRate = 1;
        }

        if (Bindings != null)
        {
            var generated = Bindings.Select(s => s.Generate(model)).ToList();
            model.Bindings.Clear();
            model.Bindings.AddRange(generated);
            model.UpdateErrors();
        }

        if (model.UsbHostEnabled) model.UsbHostDp = UsbHostDp;

        model.LedType = LedType;
        model.LedCount = LedCount < 1 ? (byte) 1 : LedCount;
        model.WtSensitivity = WtSensitivity;
        model.MouseMovementType = MouseMovementType;
        model.CombinedStrumDebounce = CombinedStrumDebounce;

        if (!model.IsApa102) return;
        model.Apa102Mosi = Apa102Mosi;
        model.Apa102Sck = Apa102Sck;
    }
}