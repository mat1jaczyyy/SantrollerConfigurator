using System;
using System.Drawing.Printing;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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

    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte? ledIndex,
        float multiplier, int offset,
        int deadZone, string name, TriggerDelegate triggerDelegate) : base(model, input, ledOn, ledOff, ledIndex, name)
    {
        Input = input;
        DirectInput? di = input!.InnermostInput() as DirectInput;
        _trigger = this.WhenAnyValue(x => x.Model.DeviceType).Select(d =>
            {
                var ret = triggerDelegate(d);
                if (di != null)
                {
                    di.IsUintDirect = ret;
                }

                return ret;
            })
            .ToProperty(this, x => x.Trigger);
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        DeadZone = deadZone;
        //Direct Input doesn't actually have a preference for uint or int, so just use the trigger value in that case.
        _inputIsUInt = this.WhenAnyValue(x => x.Input, x => x.Trigger).Select(i =>
                i.Item1!.InnermostInput() is DirectInput ? i.Item2 : i.Item1!.IsUint)
            .ToProperty(this, x => x.InputIsUint);
        _calibrationWatcher = this.WhenAnyValue(x => x.Input!.RawValue).Select(ApplyCalibration);
        _valueRawLower = this.WhenAnyValue(x => x.ValueRaw).Select(s => (s < 0 ? -s : 0))
            .ToProperty(this, x => x.ValueRawLower);
        _valueRawUpper = this.WhenAnyValue(x => x.ValueRaw).Select(s => (s > 0 ? s : 0))
            .ToProperty(this, x => x.ValueRawUpper);
        
        _value = this
            .WhenAnyValue(x => x.ValueRaw, x => x.Multiplier, x => x.Offset, x => x.DeadZone, x => x.Trigger,
                x => x.Model.DeviceType).Select(Calculate).ToProperty(this, x => x.Value);
        _valueLower = this.WhenAnyValue(x => x.Value).Select(s => (s < 0 ? -s : 0)).ToProperty(this, x => x.ValueLower);
        _valueUpper = this.WhenAnyValue(x => x.Value).Select(s => (s > 0 ? s : 0)).ToProperty(this, x => x.ValueUpper);
        _deadZoneScaled = this.WhenAnyValue(x => x.DeadZone, x => x.Trigger)
            .Select(s => Math.Min((float) s.Item1 / (s.Item2 ? ushort.MaxValue : short.MaxValue) * 500, 500f))
            .ToProperty(this, x => x.DeadZoneScaled);
        _computedDeadZoneMargin = this.WhenAnyValue(x => x.Offset, x => x.InputIsUint, x => x.DeadZone)
            .Select(s =>
            {
                if (s.Item2)
                {
                    return new Thickness((double) s.Item1 / ushort.MaxValue * 500, 0, 0, 0);
                }
                var val =  (s.Item1 + short.MaxValue) - (s.Item3);
                return new Thickness((double) val / ushort.MaxValue * 500, 0, 0, 0);
            })
            .ToProperty(this, x => x.ComputedDeadZoneMargin);
    }

    private int _calibrationMin;
    private int _calibrationMax;

    private int ApplyCalibration(int rawValue)
    {
        switch (_calibrationState)
        {
            case OutputAxisCalibrationState.MIN:
                _calibrationMin = rawValue;
                break;
            case OutputAxisCalibrationState.MAX:
                _calibrationMax = rawValue;
                break;
            case OutputAxisCalibrationState.DEADZONE:
                var min = Math.Min(_calibrationMin, _calibrationMax);
                var max = Math.Max(_calibrationMin, _calibrationMax);
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
                
                // For Uint, the deadzone goes from min to max, while for int, it starts in the middle and grows in either direction.
                _deadZone = InputIsUint ? (min - deadZone) : Math.Abs((min + max)/2 - deadZone);
                this.RaisePropertyChanged(nameof(DeadZone));
                break;
        }

        float val = Math.Min(_calibrationMin, _calibrationMax);
        if (!InputIsUint)
        {
            val += short.MaxValue;
        }

        val = val / ushort.MaxValue * 500;
        CalibrationMinMaxMargin = new Thickness(val, 0, 0, 0);
        CalibrationMinMaxWidth =
            Math.Min(
                (float) Math.Abs(_calibrationMax - _calibrationMin) / (ushort.MaxValue) * 500,
                500f);
        this.RaisePropertyChanged(nameof(CalibrationMinMaxMargin));
        this.RaisePropertyChanged(nameof(CalibrationMinMaxWidth));
        return rawValue;
    }

    public void Calibrate()
    {
        if (!SupportsCalibration())
        {
            return;
        }

        if (_calibrationState == OutputAxisCalibrationState.NONE)
        {
            _calibrationMax = (InputIsUint ? ushort.MaxValue : short.MaxValue);
            ApplyCalibration(ValueRaw);
        }

        _calibrationState++;

        if (_calibrationState == OutputAxisCalibrationState.DEADZONE)
        {
            _offset = (int) ((float) (_calibrationMax + _calibrationMin) / 2);
            Multiplier = ushort.MaxValue / (float) (_calibrationMax - _calibrationMin);
            this.RaisePropertyChanged(nameof(Offset));
        }

        if (_calibrationState == OutputAxisCalibrationState.LAST)
        {
            _calibrationState = OutputAxisCalibrationState.NONE;
        }

        this.RaisePropertyChanged(nameof(CalibrationText));
    }

    //TODO: can we somehow show a line for the trigger values for analog to digital?
    //TODO: and digital to analog should just straight up not show this interface and instead just show a range slider where you set the emulated values
    private int Calculate((int, float, int, int, bool, DeviceControllerType) values)
    {
        double val = values.Item1;
        var multiplier = values.Item2;
        var offset = values.Item3;
        var deadZone = values.Item4;
        var trigger = values.Item5;
        val -= offset;
        if (InputIsUint)
        {
            if (val < deadZone)
            {
                return 0;
            }

            val = (val - deadZone) * (ushort.MaxValue / (ushort.MaxValue - (float) deadZone));
        }
        else
        {
            if (val < deadZone && val > -deadZone)
            {
                return 0;
            }

            val = (val - (Math.Sign(val) * deadZone)) * (short.MaxValue / (short.MaxValue - (float) deadZone));
        }

        val *= multiplier;
        if (trigger)
        {
            if (!InputIsUint)
            {
                val += short.MaxValue;
            }

            if (val > ushort.MaxValue) val = ushort.MaxValue;
            if (val < 0) val = 0;
        }
        else
        {
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
    private readonly ObservableAsPropertyHelper<float> _deadZoneScaled;
    public float DeadZoneScaled => _deadZoneScaled.Value;
    private readonly ObservableAsPropertyHelper<bool> _inputIsUInt;
    public bool InputIsUint => _inputIsUInt.Value;

    private readonly ObservableAsPropertyHelper<Thickness> _computedDeadZoneMargin;
    public Thickness ComputedDeadZoneMargin => _computedDeadZoneMargin.Value;
    public float CalibrationMinMaxWidth { get; set; }
    public Thickness CalibrationMinMaxMargin { get; set; }

    private float _multiplier;

    public float Multiplier
    {
        get => _multiplier;
        set => this.RaiseAndSetIfChanged(ref _multiplier, value);
    }

    private int _offset;

    public int Offset
    {
        get => _offset;
        set
        {
            if (Trigger)
            {
                value = Math.Max(value, 0);
                value = Math.Min(value, ushort.MaxValue - _deadZone);
            }
            else
            {
                value = Math.Max(value, short.MinValue + _deadZone);
                value = Math.Min(value, short.MaxValue - _deadZone);
            }


            this.RaiseAndSetIfChanged(ref _offset, value);
            this.RaisePropertyChanged(nameof(OffsetSlider));
            this.RaisePropertyChanged(nameof(DeadZoneSlider));
        }
    }

    private int _deadZone;

    public int DeadZone
    {
        get => _deadZone;
        set
        {
            if (value < 0)
            {
                value = 0;
            }

            this.RaiseAndSetIfChanged(ref _deadZone, value);
            if (Offset - value < short.MinValue && !InputIsUint)
            {
                Offset = short.MinValue + value;
            }

            this.RaisePropertyChanged(nameof(DeadZoneSlider));
            this.RaisePropertyChanged(nameof(OffsetSlider));
        }
    }


    private readonly ObservableAsPropertyHelper<bool> _trigger;
    public bool Trigger => _trigger.Value;

    public int DeadZoneSlider
    {
        get => DeadZone + Offset;
        set => DeadZone = value - Offset;
    }


    public int OffsetSlider
    {
        get => Offset + (Trigger ? DeadZone / 2 : 0);
        set => Offset = value - (Trigger ? DeadZone / 2 : 0);
    }

    protected abstract string GenerateOutput(bool xbox);
    public override bool IsCombined => false;
    public override bool IsStrum => false;
    private OutputAxisCalibrationState _calibrationState = OutputAxisCalibrationState.NONE;
    private readonly IObservable<int> _calibrationWatcher;

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


    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        if (shared) return "";
        string function;
        if (xbox)
        {
            function = Trigger ? "handle_calibration_xbox_trigger" : "handle_calibration_xbox";
        }
        else
        {
            function = Trigger ? "handle_calibration_ps3_trigger" : "handle_calibration_ps3";
        }

        if (InputIsUint)
        {
            function += "_uint";
        }
        else
        {
            function += "_int";
        }

        var multiplier = Multiplier;
        if (InputIsUint)
        {
            multiplier *= (ushort.MaxValue / (ushort.MaxValue - (float) DeadZone));
        }
        else
        {
            multiplier *= (short.MaxValue / (short.MaxValue - (float) DeadZone));
        }

        var mulInt = (short) (multiplier * 1024);

        return
            $"{GenerateOutput(xbox)} = {function}({Input.Generate()}, {Offset}, {mulInt}, {DeadZone});";
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        throw new NotImplementedException();
    }
}