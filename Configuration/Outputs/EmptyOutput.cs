using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class EmptyOutput : Output
{
    public EmptyOutput(ConfigViewModel model) : base(model, null, Colors.Transparent, Colors.Transparent, 0, "Empty")
    {
        _isController = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.Controller)
            .ToProperty(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);
        _isMidi = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.Midi)
            .ToProperty(this, x => x.IsMidi);

        _combinedTypes = this.WhenAnyValue(vm => vm.Model.DeviceType, vm => vm.Model.RhythmType)
            .Select(ControllerEnumConverter.GetTypes).ToProperty(this, x => x.CombinedTypes);
    }

    private readonly ObservableAsPropertyHelper<bool> _isController;
    public bool IsController => _isController.Value;
    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;
    public bool IsKeyboard => _isKeyboard.Value;
    private readonly ObservableAsPropertyHelper<bool> _isMidi;
    public bool IsMidi => _isMidi.Value;

    private readonly ObservableAsPropertyHelper<IEnumerable<object>> _combinedTypes;

    public IEnumerable<object> CombinedTypes => _combinedTypes.Value;

    public object? CombinedType
    {
        get => null;
        set => Generate(value);
    }

    private Key? _key;

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

    private MouseAxisType? _mouseAxisType;

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

    private MouseButtonType? _mouseButtonType;

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

    private void Generate(object? value)
    {
        if (Model.MicroController == null) return;
        switch (Model.EmulationType)
        {
            case EmulationType.Controller:
                if (value is SimpleType simpleType)
                {
                    switch (simpleType)
                    {
                        case SimpleType.WiiInputSimple:
                            Model.Bindings.Add(new WiiCombinedOutput(Model, Model.MicroController!));
                            break;
                        case SimpleType.Gh5NeckSimple:
                            Model.Bindings.Add(new Gh5CombinedOutput(Model, Model.MicroController!));
                            break;
                        case SimpleType.Ps2InputSimple:
                            Model.Bindings.Add(new Ps2CombinedOutput(Model, Model.MicroController!));
                            break;
                        case SimpleType.WtNeckSimple:
                            Model.Bindings.Add(new GhwtCombinedOutput(Model, Model.MicroController!));
                            break;
                        case SimpleType.DjTurntableSimple:
                            Model.Bindings.Add(new DjCombinedOutput(Model, Model.MicroController!));
                            break;
                    }
                }
                else if (value is StandardAxisType standardAxisType)
                {
                    Model.Bindings.Add(new ControllerAxis(Model, new DirectInput(Model.MicroController.GetFirstAnalogPin(), DevicePinMode.Analog, Model, Model.MicroController), Colors.Transparent, Colors.Transparent, 0,short.MinValue, short.MaxValue, 0,
                        standardAxisType));
                }
                else if (value is StandardButtonType standardButtonType)
                {
                    Model.Bindings.Add(new ControllerButton(Model, new DirectInput(0, DevicePinMode.PullUp, Model, Model.MicroController), Colors.Transparent, Colors.Transparent, 0, 5,
                        standardButtonType));
                }

                break;
            case EmulationType.KeyboardMouse:
                if (MouseAxisType.HasValue)
                {
                    Model.Bindings.Add(new MouseAxis(Model, null, Colors.Transparent, Colors.Transparent, 0, 1, 0, 0,
                        MouseAxisType.Value));
                }
                else if (MouseButtonType.HasValue)
                {
                    Model.Bindings.Add(new MouseButton(Model, null, Colors.Transparent, Colors.Transparent, 0,5,
                        MouseButtonType.Value));
                }
                else if (Key.HasValue)
                {
                    Model.Bindings.Add(new KeyboardButton(Model, null, Colors.Transparent, Colors.Transparent, 0,5,
                        Key.Value));
                }

                break;
        }

        Dispatcher.UIThread.InvokeAsync(() => Model.Bindings.Remove(this));
    }

    public override string? GetLocalisedName() => "Unset";
    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public virtual bool RequiresInput => false;

    public override SerializedOutput Serialize()
    {
        throw new IncompleteConfigurationException("Output is not configured!");
    }

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        throw new IncompleteConfigurationException("Output is not configured!");
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        throw new IncompleteConfigurationException("Output is not configured!");
    }
}