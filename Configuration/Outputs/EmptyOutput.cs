using System;
using System.Collections.Generic;
using System.IO;
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
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class EmptyOutput : Output
{
    private readonly ObservableAsPropertyHelper<IEnumerable<object>> _combinedTypes;

    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;

    private Key? _key;

    private MouseAxisType? _mouseAxisType;

    private MouseButtonType? _mouseButtonType;

    public EmptyOutput(ConfigViewModel model) : base(model, new FixedInput(model, 0, false), Colors.Black, Colors.Black,
        Array.Empty<byte>(), false)
    {
        this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => Model.GetSimpleEmulationType() is EmulationType.Controller)
            .ToPropertyEx(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => Model.GetSimpleEmulationType() is EmulationType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);

        _combinedTypes = this.WhenAnyValue(vm => vm.Model.DeviceControllerType)
            .Select(type => ControllerEnumConverter.GetTypes(type).Where(s2 =>
                model.IsPico || s2 is not (SimpleType.WtNeckSimple or SimpleType.Bluetooth or SimpleType.UsbHost)))
            .ToProperty(this, x => x.CombinedTypes);
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsController { get; }
    public override bool IsKeyboard => _isKeyboard.Value;

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

    public override string ErrorText => Resources.ErrorInputUnbound;

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
                    SimpleType.Led => new Led(Model, !Model.IsApa102, false, -1,
                        Colors.Black, Colors.Black,
                        Array.Empty<byte>(),
                        Enum.GetValues<LedCommandType>().Where(Led.FilterLeds((Model.DeviceControllerType,
                            Model.EmulationType,
                            Model.IsApa102))).First(), 0, 0),
                    SimpleType.Rumble => new Rumble(Model, -1,
                        RumbleMotorType.Left),
                    SimpleType.ConsoleMode => new EmulationMode(Model,
                        new DirectInput(-1, false, DevicePinMode.PullUp, Model),
                        EmulationModeType.XboxOne),
                    SimpleType.Reset => new Reset(Model,
                        new DirectInput(-1, false, DevicePinMode.PullUp, Model)),
                    SimpleType.UsbHost => new UsbHostCombinedOutput(Model),
                    SimpleType.Bluetooth => new BluetoothOutput(Model, ""),
                    _ => null
                },
                StandardAxisType standardAxisType => new ControllerAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0,
                    ushort.MaxValue, standardAxisType, false),
                StandardButtonType standardButtonType => new ControllerButton(Model,
                    new DirectInput(-1, false, DevicePinMode.PullUp, Model),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType, false),
                InstrumentButtonType standardButtonType => new GuitarButton(Model,
                    new DirectInput(-1, false, DevicePinMode.PullUp, Model),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 5,
                    standardButtonType, false),
                DrumAxisType drumAxisType => new DrumAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0, 10, drumAxisType, false),
                Ps3AxisType ps3AxisType => new Ps3Axis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0,
                    ps3AxisType),
                GuitarAxisType.Slider => new GuitarAxis(Model,
                    new GhWtTapInput(GhWtInputType.TapBar, Model, -1,
                        -1, -1,
                        -1),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0, GuitarAxisType.Slider, false),
                GuitarAxisType guitarAxisType => new GuitarAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0, guitarAxisType, false),
                DjAxisType.LeftTableVelocity => new DjAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.LeftTableVelocity, false),
                DjAxisType.RightTableVelocity => new DjAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.RightTableVelocity, false),
                DjAxisType.EffectsKnob => new DjAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    1, DjAxisType.EffectsKnob, false),
                DjAxisType djAxisType => new DjAxis(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(),
                    ushort.MinValue, ushort.MaxValue, 0, djAxisType, false),
                DjInputType djInputType => new DjButton(Model,
                    new DirectInput(-1, false, DevicePinMode.Analog, Model),
                    Colors.Black, Colors.Black, Array.Empty<byte>(), 10,
                    djInputType, false),
                _ => null
            },

            EmulationType.KeyboardMouse => this switch
            {
                {MouseAxisType: not null} => new MouseAxis(Model, new FixedInput(Model, 0, false), Colors.Black,
                    Colors.Black,
                    Array.Empty<byte>(), 1, 0, 0,
                    MouseAxisType.Value),
                {MouseButtonType: not null} => new MouseButton(Model, new FixedInput(Model, 0, false), Colors.Black,
                    Colors.Black,
                    Array.Empty<byte>(), 5,
                    MouseButtonType.Value),
                {Key: not null} => new KeyboardButton(Model, new FixedInput(Model, 0, false), Colors.Black,
                    Colors.Black,
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
            if (output is CombinedOutput combinedOutput)
            {
                combinedOutput.SetOutputsOrDefaults(Array.Empty<Output>());
            }
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

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return "";
    }

    public override Enum GetOutputType()
    {
        return EmptyType.Empty;
    }


    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        throw new IncompleteConfigurationException(ErrorText);
    }
}