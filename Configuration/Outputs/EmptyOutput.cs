using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class EmptyOutput : Output
{
    private readonly ObservableAsPropertyHelper<IEnumerable<object>> _combinedTypes;

    private readonly ObservableAsPropertyHelper<bool> _isController;
    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;

    private Key? _key;

    private MouseAxisType? _mouseAxisType;

    private MouseButtonType? _mouseButtonType;

    public EmptyOutput(ConfigViewModel model) : base(model, new FixedInput(model, 0), Colors.Black, Colors.Black,
        Array.Empty<byte>())
    {
        _isController = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => Model.GetSimpleEmulationType() is EmulationType.Controller)
            .ToProperty(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => Model.GetSimpleEmulationType() is EmulationType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);

        _combinedTypes = this.WhenAnyValue(vm => vm.Model.UsbHostEnabled,  vm => vm.Model.DeviceType,
                vm => vm.Model.RhythmType)
            .Select(tuple => ControllerEnumConverter.GetTypes((tuple.Item2, tuple.Item3)).Where(s2 => (model.IsPico || s2 is not (SimpleType.WtNeckSimple or SimpleType.Bluetooth)) && tuple.Item1 || s2 is not SimpleType.UsbHost)).ToProperty(this, x => x.CombinedTypes);
    }

    public virtual bool IsController => _isController.Value;
    public override bool IsKeyboard => _isKeyboard.Value;

    public override bool Valid => true;

    public IEnumerable<object> CombinedTypes => _combinedTypes.Value;

    public object? CombinedType
    {
        get => null;
        set => Generate(value);
    }

    public Key? Key
    {
        get => _key;
        set
        {
            this.RaiseAndSetIfChanged(ref _key, value);
            this.RaiseAndSetIfChanged(ref _mouseAxisType, null, nameof(MouseAxisType));
            this.RaiseAndSetIfChanged(ref _mouseButtonType, null, nameof(MouseButtonType));
        }
    }

    public IEnumerable<Key> Keys => Enum.GetValues<Key>();

    public MouseAxisType? MouseAxisType
    {
        get => _mouseAxisType;
        set
        {
            this.RaiseAndSetIfChanged(ref _mouseAxisType, value);
            this.RaiseAndSetIfChanged(ref _mouseButtonType, null, nameof(MouseButtonType));
            this.RaiseAndSetIfChanged(ref _key, null, nameof(Key));
        }
    }

    public IEnumerable<MouseAxisType> MouseAxisTypes => Enum.GetValues<MouseAxisType>();

    public MouseButtonType? MouseButtonType
    {
        get => _mouseButtonType;
        set
        {
            this.RaiseAndSetIfChanged(ref _mouseButtonType, value);
            this.RaiseAndSetIfChanged(ref _mouseAxisType, null, nameof(MouseAxisType));
            this.RaiseAndSetIfChanged(ref _key, null, nameof(Key));
        }
    }

    public IEnumerable<MouseButtonType> MouseButtonTypes => Enum.GetValues<MouseButtonType>();

    public override string ErrorText => "Input is not bound!";

    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    private void Generate(object? value)
    {
        Output? output = Model.GetSimpleEmulationType() switch
        {
            EmulationType.Controller => value switch
            {
                SimpleType simpleType => simpleType switch
                {
                    SimpleType.WiiInputSimple => new WiiCombinedOutput(Model),
                    SimpleType.Gh5NeckSimple => new Gh5CombinedOutput(Model),
                    SimpleType.Ps2InputSimple => new Ps2CombinedOutput(Model),
                    SimpleType.WtNeckSimple => new GhwtCombinedOutput(Model),
                    SimpleType.DjTurntableSimple => new DjCombinedOutput(Model),
                    SimpleType.Led => new Led(Model, !Model.IsApa102, 0, Colors.Black, Colors.Black, Array.Empty<byte>(),
                        Enum.GetValues<RumbleCommand>().Where(Led.FilterLeds((Model.DeviceType, Model.EmulationType, Model.RhythmType, Model.IsApa102))).First()),
                    SimpleType.Rumble => new Rumble(Model, 0, RumbleMotorType.Left),
                    SimpleType.ConsoleMode => new EmulationMode(Model,
                        new DirectInput(Model.Microcontroller.GetFirstDigitalPin(), DevicePinMode.PullUp, Model),
                        EmulationModeType.XboxOne),
                    SimpleType.RfSimple => new RfRxOutput(Model, 1, 1, RfPowerLevel.Min, RfDataRate.One),
                    SimpleType.UsbHost => new UsbHostInput(Model),
                    SimpleType.Bluetooth => new BluetoothOutput(Model, ""),
                    _ => null
                },
                StandardAxisType standardAxisType => new ControllerAxis(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    short.MinValue, short.MaxValue, 0,
                    standardAxisType),
                StandardButtonType standardButtonType => new ControllerButton(Model,
                    new DirectInput(0, DevicePinMode.PullUp, Model), Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType),
                InstrumentButtonType standardButtonType => new GuitarButton(Model,
                    new DirectInput(0, DevicePinMode.PullUp, Model), Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType),
                DrumAxisType drumAxisType => new DrumAxis(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    short.MinValue, short.MaxValue, 0,
                    1000, 10, drumAxisType),
                Ps3AxisType ps3AxisType => new Ps3Axis(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    short.MinValue, short.MaxValue, 0,
                    ps3AxisType),
                GuitarAxisType guitarAxisType => new GuitarAxis(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    short.MinValue, short.MaxValue, 0, guitarAxisType),
                DjAxisType djAxisType => new DjAxis(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    short.MinValue, short.MaxValue, 0, djAxisType),
                DjInputType djInputType => new DjButton(Model,
                    new DirectInput(Model.Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(), 10,
                    djInputType),
                _ => null
            },

            EmulationType.KeyboardMouse => this switch
            {
                {MouseAxisType: not null} => new MouseAxis(Model, new FixedInput(Model, 0), Colors.Black, Colors.Black,
                    Array.Empty<byte>(), 1, 0, 0,
                    MouseAxisType.Value),
                {MouseButtonType: not null} => new MouseButton(Model, new FixedInput(Model, 0), Colors.Black,
                    Colors.Black,
                    Array.Empty<byte>(), 5,
                    MouseButtonType.Value),
                {Key: not null} => new KeyboardButton(Model, new FixedInput(Model, 0), Colors.Black, Colors.Black,
                    Array.Empty<byte>(), 5,
                    Key.Value),
                _ => null
            },
            _ => null
        };
        if (output != null)
        {
            output.Expanded = true;
            Model.Bindings.Add(output);
        }

        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            Model.Bindings.Remove(this);
            Model.UpdateErrors();
        });
    }

    public override void UpdateBindings()
    {
    }

    public override SerializedOutput Serialize()
    {
        throw new IncompleteConfigurationException(ErrorText);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "";
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Generic.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce)
    {
        throw new IncompleteConfigurationException("Unconfigured output");
    }
}