using System;
using System.Drawing.Printing;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public enum OutputAxisCalibrationState
{
    NONE,
    MIN,
    MAX,
    DEADZONE,
    LAST
}

public abstract class OutputAxis : Output
{
    protected delegate bool TriggerDelegate(DeviceControllerType type);

    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte ledIndex,
        int min, int max,
        int deadZone, string name, TriggerDelegate triggerDelegate) : base(model, input, ledOn, ledOff, ledIndex, name)
    {
        Input = input;
        var di = input!.InnermostInput() as DirectInput;
        _trigger = this.WhenAnyValue(x => x.Model.DeviceType).Select(d =>
            {
                var ret = triggerDelegate(d);
                di?.SetTrigger(ret);
                return ret;
            })
            .ToProperty(this, x => x.Trigger);
        LedOn = ledOn;
        LedOff = ledOff;
        _calibrationMax = max;
        _calibrationMin = min;
        DeadZone = deadZone;
        //Direct Input doesn't actually have a preference for uint or int, so just use the trigger value in that case.
        _inputIsUInt = this.WhenAnyValue(x => x.Input, x => x.Trigger).Select(i =>
                i.Item1!.InnermostInput() is DirectInput ? i.Item2 : i.Item1!.IsUint)
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
        _computedDeadZoneMargin = this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint, x => x.DeadZone)
            .Select(ComputeDeadZoneMargin).ToProperty(this, x => x.ComputedDeadZoneMargin);
        _computedMinMaxMargin = this.WhenAnyValue(x => x.Min, x => x.Max, x => x.InputIsUint)
            .Select(ComputeMinMaxMargin).ToProperty(this, x => x.CalibrationMinMaxMargin);
        _isDigitalToAnalog = this.WhenAnyValue(x => x.Input).Select(s => s is DigitalToAnalog)
            .ToProperty(this, x => x.IsDigitalToAnalog);
    }

    private Thickness ComputeDeadZoneMargin((int, int, bool, int) s)
    {
        if (!s.Item3)
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
                max = min + s.Item4;
            }
            else
            {
                min = max - s.Item4;
            }
        }
        else
        {
            var mid = (max + min) / 2;
            min = mid - s.Item4;
            max = mid + s.Item4;
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
            case OutputAxisCalibrationState.MIN:
                Min = rawValue;
                break;
            case OutputAxisCalibrationState.MAX:
                Max = rawValue;
                break;
            case OutputAxisCalibrationState.DEADZONE:
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

                if (InputIsUint)
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
                    DeadZone = Math.Abs((min + max) / 2 - deadZone);
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
        if (_calibrationState == OutputAxisCalibrationState.LAST)
        {
            _calibrationState = OutputAxisCalibrationState.NONE;
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
        if (InputIsUint)
        {
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
            if (!InputIsUint)
            {
                val += short.MaxValue;
            }

            if (val > ushort.MaxValue) val = ushort.MaxValue;
            if (val < 0) val = 0;
        }
        else
        {
            val = (val - min) / (max - min) * (short.MaxValue - short.MinValue) + short.MinValue;
            if (InputIsUint)
            {
                val -= short.MaxValue;
            }

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


    private readonly ObservableAsPropertyHelper<bool> _trigger;
    public bool Trigger => _trigger.Value;
    protected abstract string GenerateOutput(bool xbox);
    public override bool IsCombined => false;
    public override bool IsStrum => false;
    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.NONE;
    private readonly ObservableAsPropertyHelper<bool> _isDigitalToAnalog;
    public bool IsDigitalToAnalog => _isDigitalToAnalog.Value;

    public string? CalibrationText => GetCalibrationText();
    protected abstract string MinCalibrationText();
    protected abstract string MaxCalibrationText();
    protected abstract bool SupportsCalibration();

    private string? GetCalibrationText()
    {
        return _calibrationState switch
        {
            OutputAxisCalibrationState.MIN => MinCalibrationText(),
            OutputAxisCalibrationState.MAX => MaxCalibrationText(),
            OutputAxisCalibrationState.DEADZONE => "Set Deadzone",
            _ => null
        };
    }

    private const string Ps3GuitarTilt = "report->accel[0]";

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        if (shared) return "";
        var tiltForPs3 = !xbox && Model.DeviceType == DeviceControllerType.Guitar &&
                         this is ControllerAxis {Type: StandardAxisType.RightStickY};

        if (Input is DigitalToAnalog)
        {
            // No calibration, we just want raw values here
            if (!tiltForPs3)
            {
                return $"{GenerateOutput(xbox)} = {Input.Generate(xbox)};";
            }

            //Thanks to clone hero, we need to invert the tilt axis for only hid
            var hidInput = (DigitalToAnalog) Input.Serialise().Generate(Model.MicroController!, Model);
            hidInput.On = -hidInput.On;
            hidInput.Off = -hidInput.Off;
            var retPs3Dta = $"{Ps3GuitarTilt} = {Input.Generate(true)};";
            var retHidDta = $"{GenerateOutput(xbox)} = {hidInput.Generate(xbox)};";
            return $"if (consoleType == PS3) {{{retPs3Dta}}} else {{{retHidDta}}}";
        }

        string function;
        if (xbox)
        {
            function = Trigger ? "handle_calibration_xbox_trigger" : "handle_calibration_xbox";
        }
        else
        {
            function = Trigger ? "handle_calibration_ps3_trigger" : "handle_calibration_ps3";
        }

        var min = Min;
        var max = Max;
        if (InputIsUint)
        {
            if (max <= min)
            {
                min -= DeadZone;
            }
        }
        else
        {
            min += DeadZone;
            max -= DeadZone;
        }

        float multiplier;
        if (Trigger)
        {
            multiplier = 1f / (max - min) * (ushort.MaxValue);
        }
        else
        {
            multiplier = 1f / (max - min) * (short.MaxValue - short.MinValue);
        }

        var mulInt = (short) (multiplier * 1024);
        var ret = InputIsUint
            ? $"{GenerateOutput(xbox)} = {function}_uint({Input.Generate(xbox)}, {min}, {mulInt}, {DeadZone});"
            : $"{GenerateOutput(xbox)} = {function}_int({Input.Generate(xbox)}, {(max + min) / 2}, {min}, {mulInt}, {DeadZone});";
        if (!tiltForPs3) return ret;
        //Funnily enough, we actually want the xbox version, as the tilt axis is 16 bit
        var retPs3Gh =
            InputIsUint
                ? $"{Ps3GuitarTilt} = {function}_uint({Input.Generate(true)}, {min}, {mulInt}, {DeadZone});"
                : $"{Ps3GuitarTilt} = {function}_int({Input.Generate(true)}, {(max + min) / 2}, {min}, {mulInt}, {DeadZone});";
        return $"if (consoleType == PS3) {{{retPs3Gh}}} else {{{ret}}}";
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        throw new NotImplementedException();
    }
}