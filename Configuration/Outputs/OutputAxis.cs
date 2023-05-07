using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public enum OutputAxisCalibrationState
{
    None,
    Min,
    Max,
    DeadZone,
    Last
}

public abstract partial class OutputAxis : Output
{
    protected const float ProgressWidth = 400;

    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.None;

    protected OutputAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        int min, int max,
        int deadZone, bool trigger, bool childOfCombined) : base(model, new FixedInput(model, 0), ledOn, ledOff,
        ledIndices, childOfCombined)
    {
        Input = input;
        Trigger = trigger;
        LedOn = ledOn;
        LedOff = ledOff;
        Max = max;
        Min = min;
        DeadZone = deadZone;
        this.WhenAnyValue(x => x.Input).Select(i => i is {IsUint: true})
            .ToPropertyEx(this, x => x.InputIsUint);
        var calibrationWatcher = this.WhenAnyValue(x => x.Input.RawValue);
        calibrationWatcher.Subscribe(ApplyCalibration);
        this.WhenAnyValue(x => x.ValueRaw).Select(s => s < 0 ? -s : 0)
            .ToPropertyEx(this, x => x.ValueRawLower);
        this.WhenAnyValue(x => x.ValueRaw).Select(s => s > 0 ? s : 0)
            .ToPropertyEx(this, x => x.ValueRawUpper);

        this
            .WhenAnyValue(x => x.Enabled, x => x.ValueRaw, x => x.Min, x => x.Max, x => x.DeadZone, x => x.Trigger,
                x => x.Model.DeviceType).Select(Calculate).ToPropertyEx(this, x => x.Value);
        this.WhenAnyValue(x => x.Value).Select(s => s < 0 ? -s : 0).ToPropertyEx(this, x => x.ValueLower);
        this.WhenAnyValue(x => x.Value).Select(s => s > 0 ? s : 0).ToPropertyEx(this, x => x.ValueUpper);
        this
            .WhenAnyValue(x => x.Min, x => x.Max, x => x.Trigger, x => x.InputIsUint, x => x.DeadZone)
            .Select(ComputeDeadZoneMargin).ToPropertyEx(this, x => x.ComputedDeadZoneMargin);
        this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint)
            .Select(ComputeMinMaxMargin).ToPropertyEx(this, x => x.CalibrationMinMaxMargin);
        this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog)
            .ToPropertyEx(this, x => x.IsDigitalToAnalog);
    }

    public float FullProgressWidth => ProgressWidth;
    public float HalfProgressWidth => ProgressWidth / 2; // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public int ValueRawLower { get; }

    [ObservableAsProperty] public int ValueRawUpper { get; }

    [ObservableAsProperty] public int Value { get; }

    [ObservableAsProperty] public int ValueLower { get; }

    [ObservableAsProperty] public int ValueUpper { get; }

    [ObservableAsProperty] public bool InputIsUint { get; }
    [ObservableAsProperty] public bool IsDigitalToAnalog { get; }

    [ObservableAsProperty] public Thickness ComputedDeadZoneMargin { get; }
    [ObservableAsProperty] public Thickness CalibrationMinMaxMargin { get; }

    // ReSharper enable UnassignedGetOnlyAutoProperty
    [Reactive] public int Min { get; set; }

    [Reactive] public int Max { get; set; }

    [Reactive] public int DeadZone { get; set; }


    public bool Trigger { get; }
    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public virtual string? CalibrationText => GetCalibrationText();

    private Thickness ComputeDeadZoneMargin((int min, int max, bool trigger, bool inputIsUint, int deadZone) s)
    {
        float min = Math.Min(s.min, s.max);
        float max = Math.Max(s.min, s.max);
        var inverted = s.min > s.max;
        if (s.trigger)
        {
            if (inverted)
                min = max - s.deadZone;
            else
                max = min + s.deadZone;
        }
        else
        {
            var mid = (max + min) / 2;
            min = mid - s.deadZone;
            max = mid + s.deadZone;
            if (!s.inputIsUint)
            {
                min += short.MaxValue;
                max += short.MaxValue;
            }
        }

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        ;
        return new Thickness(left, 0, right, 0);
    }


    private static Thickness ComputeMinMaxMargin((int min, int max, bool isUint) s)
    {
        if (!s.isUint)
        {
            s.min += short.MaxValue;
            s.max += short.MaxValue;
        }

        float min = Math.Min(s.min, s.max);
        float max = Math.Max(s.min, s.max);

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return new Thickness(left, 0, right, 0);
    }

    public virtual void ApplyCalibration(int rawValue)
    {
        switch (_calibrationState)
        {
            case OutputAxisCalibrationState.Min:
                Min = rawValue;
                break;
            case OutputAxisCalibrationState.Max:
                Max = rawValue;
                break;
            case OutputAxisCalibrationState.DeadZone:
                var min = Math.Min(Min, Max);
                var max = Math.Max(Min, Max);
                rawValue = Math.Min(Math.Max(min, rawValue), max);

                if (Trigger)
                {
                    if (Min < Max)
                        DeadZone = rawValue - min;
                    else
                        DeadZone = max - rawValue;
                }
                else
                {
                    // For non triggers, deadzone starts in the middle and grows in both directions
                    DeadZone = Math.Abs((max - min) / 2 - rawValue);
                }

                break;
        }
    }

    [RelayCommand]
    private void Calibrate()
    {
        if (!SupportsCalibration()) return;

        _calibrationState++;
        if (_calibrationState == OutputAxisCalibrationState.Last) _calibrationState = OutputAxisCalibrationState.None;

        ApplyCalibration(ValueRaw);

        this.RaisePropertyChanged(nameof(CalibrationText));
    }

    private int Calculate(
        (bool enabled, int value, int min, int max, int deadZone, bool trigger, DeviceControllerType
            deviceControllerType) values)
    {
        if (!values.enabled) return 0;
        double val = values.value;

        var min = (float) values.min;
        var max = (float) values.max;
        var deadZone = (float) values.deadZone;
        var trigger = values.trigger;
        var inverted = min > max;
        if (trigger)
        {
            // Trigger is uint, so if the input is not, shove it forward to put it into the right range
            if (!InputIsUint)
            {
                val += short.MaxValue;
                min += short.MaxValue;
                max += short.MaxValue;
            }

            if (inverted)
            {
                min -= deadZone;
                if (val > min) return 0;
            }
            else
            {
                min += deadZone;
                if (val < min) return 0;
            }
        }
        else
        {
            // Standard axis is int, so if the input is not, then subtract to get it within the right range
            if (InputIsUint)
            {
                val -= short.MaxValue;
                max -= short.MaxValue;
                min -= short.MaxValue;
            }

            var deadZoneCalc = val - (max + min) / 2;
            if (deadZoneCalc < deadZone && deadZoneCalc > -deadZone) return 0;

            val -= Math.Sign(val) * deadZone;
            if (max > min)
            {
                min += deadZone;
                max -= deadZone;
            }
            else
            {
                min -= deadZone;
                max += deadZone;
            }
        }

        if (trigger)
        {
            val = (val - min) / (max - min) * ushort.MaxValue;
            if (val > ushort.MaxValue) val = ushort.MaxValue;
            if (val < 0) val = 0;
        }
        else
        {
            val = (val - min) / (max - min) * (short.MaxValue - short.MinValue) + short.MinValue;
            if (val > short.MaxValue) val = short.MaxValue;
            if (val < short.MinValue) val = short.MinValue;
        }

        return (int) val;
    }

    public abstract string GenerateOutput(ConfigField mode);

    public abstract bool ShouldFlip(ConfigField mode);

    protected abstract string MinCalibrationText();
    protected abstract string MaxCalibrationText();
    protected abstract bool SupportsCalibration();

    private string? GetCalibrationText()
    {
        return _calibrationState switch
        {
            OutputAxisCalibrationState.Min => MinCalibrationText(),
            OutputAxisCalibrationState.Max => MaxCalibrationText(),
            OutputAxisCalibrationState.DeadZone => "Set Deadzone",
            _ => null
        };
    }

    protected string GenerateAssignment(ConfigField mode, bool forceAccel, bool forceTrigger, bool whammy)
    {
        if (Input is FixedInput or DigitalToAnalog) return Input.Generate(mode);

        string function;
        var trigger = Trigger || forceTrigger;
        var normal = false;

        switch (mode)
        {
            case ConfigField.XboxOne when whammy:
                function = "handle_calibration_xbox_whammy";
                break;
            case ConfigField.XboxOne when trigger:
                function = "handle_calibration_xbox_one_trigger";
                break;
            case ConfigField.XboxOne:
                normal = true;
                function = "handle_calibration_xbox";
                break;
            case ConfigField.Xbox360 when whammy:
                function = "handle_calibration_xbox_whammy";
                break;
            case ConfigField.Xbox360 when trigger:
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.Xbox360:
                normal = true;
                function = "handle_calibration_xbox";
                break;
            // For LED stuff (Shared), we can use the standard handle_calibration_ps3 instead.
            case ConfigField.Ps3 when forceAccel:
                normal = true;
                function = "handle_calibration_ps3_accel";
                break;
            case ConfigField.Ps3 or ConfigField.Shared when whammy:
                function = "handle_calibration_ps3_whammy";
                break;
            case ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Shared when trigger:
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Shared:
                normal = true;
                function = "handle_calibration_ps3";
                break;
            default:
                return "";
        }

        if (ShouldFlip(mode)) function = "-" + function;

        var min = Min;
        var max = Max;
        var inverted = Min > Max;
        float multiplier;
        if (Trigger)
        {
            if (!InputIsUint)
            {
                max += short.MaxValue;
                min += short.MaxValue;
            }

            if (inverted)
                min -= DeadZone;
            else
                min += DeadZone;

            multiplier = 1f / (max - min) * ushort.MaxValue;
        }
        else
        {
            if (InputIsUint)
            {
                max -= short.MaxValue;
                min -= short.MaxValue;
            }

            if (inverted)
            {
                min -= DeadZone;
                max += DeadZone;
            }
            else
            {
                min += DeadZone;
                max -= DeadZone;
            }

            multiplier = 1f / (max - min) * (short.MaxValue - short.MinValue);
        }

        var generated = "(" + Input.Generate(mode);
        generated += (Trigger || forceAccel) switch
        {
            true when !InputIsUint => ") + INT16_MAX",
            false when InputIsUint => ") - INT16_MAX",
            _ => ")"
        };

        var mulInt = (short) (multiplier * 512);
        return normal
            ? $"{function}({generated}, {(max + min) / 2}, {min}, {mulInt}, {DeadZone})"
            : $"{function}({generated}, {min}, {mulInt}, {DeadZone})";
    }

    protected string CalculateLeds(ConfigField mode)
    {
        if (!AreLedsEnabled) return "";
        var ledRead = GenerateOutput(mode);
        return LedIndices.Aggregate("",
            (current, index) =>
                current +
                $@"if (!ledState[{index - 1}].select) {{{Model.LedType.GetLedAssignment(LedOn, LedOff, ledRead, index)}}}");
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce)
    {
        if (mode == ConfigField.Shared) return "";

        var output = GenerateOutput(mode);
        if (!output.Any()) return "";

        if (Input is not DigitalToAnalog dta)
            return $"{output} = {GenerateAssignment(mode, false, false, false)};{CalculateLeds(mode)}";

        // Digital to Analog stores values based on uint16_t for trigger, and int16_t for sticks
        var val = dta.On;

        switch (mode)
        {
            // x360 triggers are int16_t
            case ConfigField.Xbox360 when !Trigger:
                break;
            // xb1 triggers and axis are already of the above form
            case ConfigField.XboxOne:
                break;
            // 360 triggers, and ps3 and ps4 triggers are uint8_t
            case ConfigField.Xbox360 or ConfigField.Ps3 or ConfigField.Ps4 when Trigger:
                val >>= 8;
                break;
            // ps3 and ps4 axis are uint8_t, so we both need to shift and add 128
            case ConfigField.Ps3 or ConfigField.Ps4 when !Trigger:
                val = (val >> 8) + 128;
                break;
            // Mouse is always not a trigger, and is int8_t
            case ConfigField.Mouse:
                val >>= 8;
                break;
            default:
                return "";
        }

        return $"if ({Input.Generate(mode)}) {{{output} = {val};}}";
    }

    public override void UpdateBindings()
    {
    }
}