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
    private const float ProgressWidth = 400;

    private readonly ObservableAsPropertyHelper<Thickness> _computedDeadZoneMargin;
    private readonly ObservableAsPropertyHelper<Thickness> _computedMinMaxMargin;
    private readonly ObservableAsPropertyHelper<bool> _inputIsUInt;
    private readonly ObservableAsPropertyHelper<bool> _isDigitalToAnalog;
    private readonly ObservableAsPropertyHelper<int> _value;
    private readonly ObservableAsPropertyHelper<int> _valueLower;

    private readonly ObservableAsPropertyHelper<int> _valueRawLower;
    private readonly ObservableAsPropertyHelper<int> _valueRawUpper;
    private readonly ObservableAsPropertyHelper<int> _valueUpper;
    private int _calibrationMax;
    private int _calibrationMin;
    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.None;

    private int _deadZone;

    protected OutputAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        int min, int max,
        int deadZone, bool trigger) : base(model, new FixedInput(model, 0), ledOn, ledOff,
        ledIndices)
    {
        Input = input;
        Trigger = trigger;
        LedOn = ledOn;
        LedOff = ledOff;
        Max = max;
        Min = min;
        DeadZone = deadZone;
        _inputIsUInt = this.WhenAnyValue(x => x.Input).Select(i => i is {IsUint: true})
            .ToProperty(this, x => x.InputIsUint);
        var calibrationWatcher = this.WhenAnyValue(x => x.Input!.RawValue);
        calibrationWatcher.Subscribe(ApplyCalibration);
        _valueRawLower = this.WhenAnyValue(x => x.ValueRaw).Select(s => s < 0 ? -s : 0)
            .ToProperty(this, x => x.ValueRawLower);
        _valueRawUpper = this.WhenAnyValue(x => x.ValueRaw).Select(s => s > 0 ? s : 0)
            .ToProperty(this, x => x.ValueRawUpper);

        _value = this
            .WhenAnyValue(x => x.Enabled, x => x.ValueRaw, x => x.Min, x => x.Max, x => x.DeadZone, x => x.Trigger,
                x => x.Model.DeviceType).Select(Calculate).ToProperty(this, x => x.Value);
        _valueLower = this.WhenAnyValue(x => x.Value).Select(s => s < 0 ? -s : 0).ToProperty(this, x => x.ValueLower);
        _valueUpper = this.WhenAnyValue(x => x.Value).Select(s => s > 0 ? s : 0).ToProperty(this, x => x.ValueUpper);
        _computedDeadZoneMargin = this
            .WhenAnyValue(x => x.Min, x => x.Max, x => x.Trigger, x => x.InputIsUint, x => x.DeadZone)
            .Select(ComputeDeadZoneMargin).ToProperty(this, x => x.ComputedDeadZoneMargin);
        _computedMinMaxMargin = this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint)
            .Select(ComputeMinMaxMargin).ToProperty(this, x => x.CalibrationMinMaxMargin);
        _isDigitalToAnalog = this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog)
            .ToProperty(this, x => x.IsDigitalToAnalog);
    }

    public float FullProgressWidth => ProgressWidth;
    public float HalfProgressWidth => ProgressWidth / 2;
    public int ValueRawLower => _valueRawLower.Value;
    public int ValueRawUpper => _valueRawUpper.Value;
    public int Value => _value.Value;
    public int ValueLower => _valueLower.Value;
    public int ValueUpper => _valueUpper.Value;
    public bool InputIsUint => _inputIsUInt.Value;

    public int Min
    {
        get => _calibrationMin;
        set => this.RaiseAndSetIfChanged(ref _calibrationMin, value);
    }

    public int Max
    {
        get => _calibrationMax;
        set => this.RaiseAndSetIfChanged(ref _calibrationMax, value);
    }

    public Thickness ComputedDeadZoneMargin => _computedDeadZoneMargin.Value;
    public Thickness CalibrationMinMaxMargin => _computedMinMaxMargin.Value;

    public int DeadZone
    {
        get => _deadZone;
        set => this.RaiseAndSetIfChanged(ref _deadZone, value);
    }


    public bool Trigger { get; }
    public override bool IsCombined => false;
    public override bool IsStrum => false;
    public bool IsDigitalToAnalog => _isDigitalToAnalog.Value;

    public string? CalibrationText => GetCalibrationText();

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
            min += short.MaxValue;
            max += short.MaxValue;
        }

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        ;
        return new Thickness(left, 0, right, 0);
    }


    private static Thickness ComputeMinMaxMargin((int, int, bool) s)
    {
        if (!s.Item3)
        {
            s.Item1 += short.MaxValue;
            s.Item2 += short.MaxValue;
        }

        float min = Math.Min(s.Item1, s.Item2);
        float max = Math.Max(s.Item1, s.Item2);

        var left = Math.Min(min / ushort.MaxValue * ProgressWidth, ProgressWidth);

        var right = ProgressWidth - Math.Min(max / ushort.MaxValue * ProgressWidth, ProgressWidth);
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return new Thickness(left, 0, right, 0);
    }

    private void ApplyCalibration(int rawValue)
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
                    DeadZone = Math.Abs((min + max) / 2 - rawValue);
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
            }

            var deadZoneCalc = val - (max + min) / 2;
            if (deadZoneCalc < deadZone && deadZoneCalc > -deadZone) return 0;

            val -= Math.Sign(val) * deadZone;
            min += deadZone;
            max -= deadZone;
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

        var accel = forceAccel || this is ControllerAxis
        {
            Type: StandardAxisType.Gyro or StandardAxisType.AccelerationX or StandardAxisType.AccelerationY
            or StandardAxisType.AccelerationZ
        };
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
            case ConfigField.Ps3 when whammy:
                function = "handle_calibration_ps3_whammy";
                break;
            case ConfigField.Ps3 when accel:
                function = "handle_calibration_ps3_accel";
                break;
            case ConfigField.Ps3 when trigger:
            case ConfigField.Ps4 when trigger:
                function = "handle_calibration_ps3_360_trigger";
                break;
            case ConfigField.Ps3:
            case ConfigField.Ps4:
                normal = true;
                function = "handle_calibration_ps3";
                break;
            default:
                return "";
        }

        var min = Min;
        var max = Max;
        bool inverted = Min > Max;
        float multiplier;
        if (Trigger)
        {
            if (inverted)
            {
                min -= DeadZone;
            }
            else
            {
                min += DeadZone;
            }

            multiplier = 1f / (max - min) * ushort.MaxValue;
        }
        else
        {
            min += DeadZone;
            max -= DeadZone;
            multiplier = 1f / (max - min) * (short.MaxValue - short.MinValue);
        }

        var generated = "(" + Input.Generate(mode);
        generated += (Trigger || accel) switch
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
        var led = "";
        if (!AreLedsEnabled) return led;
        foreach (var index in LedIndices)
        {
            var ledRead = mode == ConfigField.Xbox360
                ? $"{GenerateOutput(mode)} << 8"
                : GenerateOutput(mode);
            led += $@"if (!ledState[{index - 1}].select) {{
                        {Model.LedType.GetLedAssignment(LedOn, LedOff, ledRead, index)}
                    }}";
        }

        return led;
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        var output = GenerateOutput(mode);
        if (!output.Any()) return "";
        var mask = GetMaskField(output, mode);
        if (mask.Any()) return mask;

        var led = Input is FixedInput ? "" : CalculateLeds(mode);
        return $"{output} = {GenerateAssignment(mode, false, false, false)}; {led}";
    }

    public override void UpdateBindings()
    {
    }
}