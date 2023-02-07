using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
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

public abstract class OutputAxis : Output
{
    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices,
        int min, int max,
        int deadZone, string name, bool trigger, bool dj = false) : base(model, input, ledOn, ledOff,
        ledIndices,
        name)
    {
        InputIsDj = dj;
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
        _valueRawLower = this.WhenAnyValue(x => x.ValueRaw).Select(s => (s < 0 ? -s : 0))
            .ToProperty(this, x => x.ValueRawLower);
        _valueRawUpper = this.WhenAnyValue(x => x.ValueRaw).Select(s => (s > 0 ? s : 0))
            .ToProperty(this, x => x.ValueRawUpper);

        _value = this
            .WhenAnyValue(x => x.ValueRaw, x => x.Min, x => x.Max, x => x.DeadZone, x => x.Trigger,
                x => x.Model.DeviceType).Select(Calculate).ToProperty(this, x => x.Value);
        _valueLower = this.WhenAnyValue(x => x.Value).Select(s => (s < 0 ? -s : 0)).ToProperty(this, x => x.ValueLower);
        _valueUpper = this.WhenAnyValue(x => x.Value).Select(s => (s > 0 ? s : 0)).ToProperty(this, x => x.ValueUpper);
        _computedDeadZoneMargin = this
            .WhenAnyValue(x => x.Min, x => x.Max, x => x.Trigger, x => x.InputIsUint, x => x.DeadZone)
            .Select(ComputeDeadZoneMargin).ToProperty(this, x => x.ComputedDeadZoneMargin);
        _computedMinMaxMargin = this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint)
            .Select(ComputeMinMaxMargin).ToProperty(this, x => x.CalibrationMinMaxMargin);
        _isDigitalToAnalog = this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog)
            .ToProperty(this, x => x.IsDigitalToAnalog);
    }

    private Thickness ComputeDeadZoneMargin((int, int, bool, bool, int) s)
    {
        if (!s.Item4)
        {
            s.Item1 += short.MaxValue;
            s.Item2 += short.MaxValue;
        }

        float min = Math.Min(s.Item1, s.Item2);
        float max = Math.Max(s.Item1, s.Item2);

        if (s.Item3)
        {
            if (s.Item1 < s.Item2)
            {
                max = min + s.Item5;
            }
            else
            {
                min = max - s.Item5;
            }
        }
        else
        {
            var mid = (max + min) / 2;
            min = mid - s.Item5;
            max = mid + s.Item5;
        }

        var left = Math.Min(min / (ushort.MaxValue) * 500, 500f);

        var right = 500 - Math.Min(max / (ushort.MaxValue) * 500, 500f);

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

        var left = Math.Min(min / (ushort.MaxValue) * 500, 500f);

        var right = 500 - Math.Min(max / (ushort.MaxValue) * 500, 500f);

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
                var valRaw = rawValue;
                int deadZone;
                if (valRaw < min)
                {
                    deadZone = min;
                }
                else if (valRaw > max)
                {
                    deadZone = max;
                }
                else
                {
                    deadZone = valRaw;
                }

                if (Trigger)
                {
                    if (Min < Max)
                    {
                        DeadZone = valRaw - min;
                    }
                    else
                    {
                        DeadZone = max - valRaw;
                    }
                }
                else
                {
                    // For Int, deadzone starts in the middle and grows in both directions
                    DeadZone = Math.Abs(((min + max) / 2) - deadZone);
                }

                break;
        }
    }

    public void Calibrate()
    {
        if (!SupportsCalibration())
        {
            return;
        }

        _calibrationState++;
        if (_calibrationState == OutputAxisCalibrationState.Last)
        {
            _calibrationState = OutputAxisCalibrationState.None;
        }

        ApplyCalibration(ValueRaw);

        this.RaisePropertyChanged(nameof(CalibrationText));
    }

    private int Calculate((int, int, int, int, bool, DeviceControllerType) values)
    {
        double val = values.Item1;

        var min = (float) values.Item2;
        var max = (float) values.Item3;
        var deadZone = (float) values.Item4;
        var trigger = values.Item5;
        if (InputIsDj)
        {
            return (int) (val * max);
        }

        if (trigger)
        {
            if (!InputIsUint)
            {
                val += short.MaxValue;
                min += short.MaxValue;
                max += short.MaxValue;
            }

            if (max > min)
            {
                if ((val - min) < deadZone)
                {
                    return 0;
                }
            }
            else
            {
                min -= deadZone;

                if (val > min)
                {
                    return 0;
                }
            }
        }
        else
        {
            if (InputIsUint)
            {
                val -= short.MaxValue;
                min -= short.MaxValue;
                max -= short.MaxValue;
            }

            var deadZoneCalc = val - ((max + min) / 2);
            if (deadZoneCalc < deadZone && deadZoneCalc > -deadZone)
            {
                return 0;
            }

            val = (val - (Math.Sign(val) * deadZone));
            min += deadZone;
            max -= deadZone;
        }

        if (trigger)
        {
            val = (val - min) / (max - min) * (ushort.MaxValue);
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

    private readonly ObservableAsPropertyHelper<int> _valueRawLower;
    public int ValueRawLower => _valueRawLower.Value;
    private readonly ObservableAsPropertyHelper<int> _valueRawUpper;
    public int ValueRawUpper => _valueRawUpper.Value;
    private readonly ObservableAsPropertyHelper<int> _value;
    public int Value => _value.Value;
    private readonly ObservableAsPropertyHelper<int> _valueLower;
    public int ValueLower => _valueLower.Value;
    private readonly ObservableAsPropertyHelper<int> _valueUpper;
    public int ValueUpper => _valueUpper.Value;
    private readonly ObservableAsPropertyHelper<bool> _inputIsUInt;
    public bool InputIsUint => _inputIsUInt.Value;
    public bool InputIsDj { get; }
    private int _calibrationMin;
    private int _calibrationMax;

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

    private readonly ObservableAsPropertyHelper<Thickness> _computedDeadZoneMargin;
    public Thickness ComputedDeadZoneMargin => _computedDeadZoneMargin.Value;
    private readonly ObservableAsPropertyHelper<Thickness> _computedMinMaxMargin;
    public Thickness CalibrationMinMaxMargin => _computedMinMaxMargin.Value;

    private int _deadZone;

    public int DeadZone
    {
        get => _deadZone;
        set => this.RaiseAndSetIfChanged(ref _deadZone, value);
    }


    public bool Trigger { get; }
    public abstract string GenerateOutput(DeviceEmulationMode mode);
    public override bool IsCombined => false;
    public override bool IsStrum => false;
    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.None;
    private readonly ObservableAsPropertyHelper<bool> _isDigitalToAnalog;
    public bool IsDigitalToAnalog => _isDigitalToAnalog.Value;

    public string? CalibrationText => GetCalibrationText();

    public int DjValue
    {
        get => Max;
        set => _calibrationMax = InputIsDj ? value : _calibrationMax;
    }

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
    protected string GenerateAssignment(DeviceEmulationMode mode, bool forceAccel, bool forceTrigger, bool whammy)
    {
        switch (Input)
        {
            case null:
                throw new IncompleteConfigurationException("Missing input!");
            case FixedInput or DigitalToAnalog:
                return Input.Generate(mode);
        }

        if (InputIsDj)
        {
            var gen = $"({Input.Generate(mode)} * {Max})";
            return mode == DeviceEmulationMode.Xbox360 ? gen : $"{gen} + {sbyte.MaxValue}";
        }

        var accel = forceAccel || this is ControllerAxis
        {
            Type: StandardAxisType.Gyro or StandardAxisType.AccelerationX or StandardAxisType.AccelerationY
            or StandardAxisType.AccelerationZ
        };
        string function;
        bool trigger = Trigger || forceTrigger;
        var normal = false;

        switch (mode)
        {
            case DeviceEmulationMode.XboxOne when whammy:
                function = "handle_calibration_xbox_whammy";
                break;
            case DeviceEmulationMode.XboxOne when trigger:
                function = "handle_calibration_xbox_one_trigger";
                break;
            case DeviceEmulationMode.XboxOne:
                normal = true;
                function = "handle_calibration_xbox";
                break;
            case DeviceEmulationMode.Xbox360 when whammy:
                function = "handle_calibration_xbox_whammy";
                break;
            case DeviceEmulationMode.Xbox360 when trigger:
                function = "handle_calibration_ps3_360_trigger";
                break;
            case DeviceEmulationMode.Xbox360:
                normal = true;
                function = "handle_calibration_xbox";
                break;
            case DeviceEmulationMode.Ps3 when whammy:
                function = "handle_calibration_ps3_whammy";
                break;
            case DeviceEmulationMode.Ps3 when accel:
                function = "handle_calibration_ps3_accel";
                break;
            case DeviceEmulationMode.Ps3 when trigger:
                function = "handle_calibration_ps3_trigger";
                break;
            case DeviceEmulationMode.Ps3:
                normal = true;
                function = "handle_calibration_ps3";
                break;
            default:
                return "";
        }

        var min = Min;
        var max = Max;
        float multiplier;
        if (Trigger)
        {
            if (!InputIsUint)
            {
                min += short.MaxValue;
                max += short.MaxValue;
            }

            if (max <= min)
            {
                min -= DeadZone;
            }

            multiplier = 1f / (max - min) * (ushort.MaxValue);
        }
        else
        {
            if (InputIsUint)
            {
                min -= short.MaxValue;
                max -= short.MaxValue;
            }

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

    protected string CalculateLeds(DeviceEmulationMode mode)
    {
        var led = "";
        if (!AreLedsEnabled) return led;
        foreach (var index in LedIndices)
        {
            var ledRead = mode == DeviceEmulationMode.Xbox360
                ? $"{GenerateOutput(mode)} << 8"
                : GenerateOutput(mode);
            led += $@"if (!ledState[{index}].select) {{
                        {string.Join("", Model.LedType.GetColors(LedOn).Zip(Model.LedType.GetColors(LedOff), new[] {'r', 'g', 'b'}).Select(b => $"ledState[{index}].{b.Third} = (uint8_t)({b.First} + ((int16_t)({b.Second - b.First} * ({ledRead})) >> 8));"))}
                    }}";
        }

        return led;
    }

    public override string Generate(DeviceEmulationMode mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (Input == null) throw new IncompleteConfigurationException("Missing input!");
        if (mode == DeviceEmulationMode.Shared) return "";
        var led = Input is FixedInput ? "" : CalculateLeds(mode);
        return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)}; {led}";
    }

    public override void UpdateBindings()
    {
    }
}