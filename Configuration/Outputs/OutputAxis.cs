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

public abstract class OutputAxis : Output
{
    protected delegate bool TriggerDelegate(DeviceControllerType type);

    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int? ledIndex, float multiplier, int offset,
        int deadZone, string name, TriggerDelegate triggerDelegate) : base(model, input, ledOn, ledOff, ledIndex, name)
    {
        _trigger = this.WhenAnyValue(x => x.Model.DeviceType).Select(d => triggerDelegate(d))
            .ToProperty(this, x => x.Trigger);
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        DeadZone = deadZone;
        //Direct Input doesn't actually have a preference for uint or int, so just use the trigger value in that case.
        _inputIsUInt = this.WhenAnyValue(x => x.Input, x => x.Trigger).Select(i =>
                i.Item1!.InnermostInput() is DirectInput ? i.Item2 : i.Item1!.IsUint)
            .ToProperty(this, x => x.InputIsUint);
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
            .Select(s => (Math.Min((float) s.Item1 / (s.Item2 ? ushort.MaxValue : short.MaxValue) * 500, 500f)))
            .ToProperty(this, x => x.DeadZoneScaled);
        _computedDeadZoneMargin = this.WhenAnyValue(x => x.Offset, x => x.Trigger)
            .Select(s =>
            {
                var val = Math.Abs((double) s.Item1) / (s.Item2 ? ushort.MaxValue : short.MaxValue) * 500;
                return s.Item1 > 0 ? new Thickness(val, 0, 0, 0) : new Thickness(0, 0, val, 0);
            })
            .ToProperty(this, x => x.ComputedDeadZoneMargin);
    }

    //TODO: can we somehow show a line for the trigger values for analog to digital?
    //TODO: and digital to analog should just straight up not show this interface and instead just show a range slider where you set the emulated values
    //TODO: when bringing up a calibration dialog, localise things to the controller input
    //for example, instead of min and max say left and right or up and down
    //For triggers, we would tell them to leave the axis alone and then push it in
    //We could even sample values for a few seconds when released, and then generate a deadzone value
    private double Calculate((double, float, int, int, bool, DeviceControllerType) values)
    {
        var val = values.Item1;
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

            val = (val - deadZone) * (ushort.MaxValue / (ushort.MaxValue - (float)deadZone));
        }
        else
        {
            if (val < deadZone && val > -deadZone)
            {
                return 0;
            }
            val = (val - (Math.Sign(val) * deadZone)) * (short.MaxValue / (short.MaxValue - (float)deadZone));
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

        return val;
    }

    //TODO: realistically, these values should actually be pulled from the Inputs, and not something stored on the output
    private double _valueRaw;

    public double ValueRaw
    {
        get => _valueRaw;
        set => this.RaiseAndSetIfChanged(ref _valueRaw, value);
    }

    private readonly ObservableAsPropertyHelper<double> _valueRawLower;
    public double ValueRawLower => _valueRawLower.Value;
    private readonly ObservableAsPropertyHelper<double> _valueRawUpper;
    public double ValueRawUpper => _valueRawUpper.Value;
    private readonly ObservableAsPropertyHelper<double> _valueLower;
    public double ValueLower => _valueLower.Value;
    private readonly ObservableAsPropertyHelper<double> _value;
    public double Value => _value.Value;
    private readonly ObservableAsPropertyHelper<double> _valueUpper;
    public double ValueUpper => _valueUpper.Value;
    private readonly ObservableAsPropertyHelper<float> _deadZoneScaled;
    public float DeadZoneScaled => _deadZoneScaled.Value;
    private readonly ObservableAsPropertyHelper<bool> _inputIsUInt;
    public bool InputIsUint => _inputIsUInt.Value;

    private readonly ObservableAsPropertyHelper<Thickness> _computedDeadZoneMargin;
    public Thickness ComputedDeadZoneMargin => _computedDeadZoneMargin.Value;

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

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        if (shared) return "";
        var isUInt = Input.InnermostInput()?.IsUint == true;
        string function;
        if (xbox)
        {
            function = Trigger ? "handle_calibration_xbox_trigger" : "handle_calibration_xbox";
        }
        else
        {
            function = Trigger ? "handle_calibration_ps3_trigger" : "handle_calibration_ps3";
        }

        if (isUInt)
        {
            function += "_uint";
        }
        else
        {
            function += "_int";
        }

        var mulInt = (int) (Multiplier * 1024);
        if (mulInt > ushort.MaxValue)
        {
            mulInt = ushort.MaxValue;
        }

        return
            $"{GenerateOutput(xbox)} = {function}({Input.Generate()}, {Offset}, {mulInt}, {DeadZone});";
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        throw new System.NotImplementedException();
    }
}