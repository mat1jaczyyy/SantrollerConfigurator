using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Rumble;

public class Rumble : Output
{

    private int _pin;

    private RumbleMotorType _rumbleMotorType;

    public Rumble(ConfigViewModel model, int pin, RumbleMotorType rumbleMotorType) : base(model,
        new FixedInput(model, 0), Colors.Black, Colors.Black, new byte[] { })
    {
        Pin = pin;
        RumbleMotorType = rumbleMotorType;
    }

    public List<int> AvailablePins => Model.Microcontroller.GetPwmPins();

    public DirectPinConfig? PinConfig { get; private set; }

    public int Pin
    {
        get => _pin;
        set
        {
            this.RaiseAndSetIfChanged(ref _pin, value);
            if (PinConfig == null) return;
            PinConfig.Pin = value;
        }
    }

    public RumbleMotorType RumbleMotorType
    {
        get => _rumbleMotorType;
        set
        {
            this.RaiseAndSetIfChanged(ref _rumbleMotorType, value);
            UpdateDetails();
        }
    }

    public IEnumerable<RumbleMotorType> RumbleMotorTypes => Enum.GetValues<RumbleMotorType>();

    public override bool IsCombined => false;
    public override bool IsStrum => false;
    public override string LedOnLabel => "";

    public override string LedOffLabel => "";

    public override bool SupportsLedOff => false;

    public override bool IsKeyboard => false;
    public override bool IsController => false;

    public override bool Valid => true;

    public override void Dispose()
    {
        if (PinConfig == null) return;
        Model.Microcontroller.UnAssignPins(PinConfig.Type);
        PinConfig = null;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Rumble Motor - " + EnumToStringConverter.Convert(RumbleMotorType);
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return PinConfig != null ? new[] {PinConfig} : Enumerable.Empty<PinConfig>();
    }

    protected override IEnumerable<DevicePin> GetOwnPins()
    {
        return new List<DevicePin>
        {
            new(Pin, DevicePinMode.Output)
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRumble(RumbleMotorType, Pin);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"Motors/{RumbleMotorType}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return mode is not ConfigField.RumbleLed
            ? ""
            : Model.Microcontroller.GenerateAnalogWrite(Pin,
                RumbleMotorType == RumbleMotorType.Left ? "rumble_left" : "rumble_right");
    }

    public override void UpdateBindings()
    {
    }
}