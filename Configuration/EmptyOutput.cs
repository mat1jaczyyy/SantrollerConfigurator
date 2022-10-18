using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;
using MouseButton = GuitarConfiguratorSharp.NetCore.Configuration.Outputs.MouseButton;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public class EmptyOutput : Output
{
    public EmptyOutput(ConfigViewModel model) : base(model, null, Colors.Transparent, Colors.Transparent, "Empty")
    {
        _isRhythm = this.WhenAnyValue(x => x.Model.DeviceType)
            .Select(x => x is DeviceControllerType.Drum or DeviceControllerType.Guitar)
            .ToProperty(this, x => x.IsRhythm);
        _isController = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.Controller)
            .ToProperty(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.KeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);
        _isMidi = this.WhenAnyValue(x => x.Model.EmulationType)
            .Select(x => x is EmulationType.Midi)
            .ToProperty(this, x => x.IsMidi);

        _combinedTypes = this.WhenAnyValue(vm => vm.Model.DeviceType, vm => vm.Model.RhythmType).Select(ControllerEnumConverter.GetTypes).ToProperty(this, x => x.CombinedTypes);
        var canAdd = this.WhenAnyValue(vm => vm.CombinedType).Select(s => s != null);
        AddCommand = ReactiveCommand.Create(Generate, canAdd);
    }

    public ICommand AddCommand { get; }

    private readonly ObservableAsPropertyHelper<bool> _isRhythm;
    public bool IsRhythm => _isRhythm.Value;
    private readonly ObservableAsPropertyHelper<bool> _isController;
    public bool IsController => _isController.Value;
    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;
    public bool IsKeyboard => _isKeyboard.Value;
    private readonly ObservableAsPropertyHelper<bool> _isMidi;
    public bool IsMidi => _isMidi.Value;

    private readonly ObservableAsPropertyHelper<IEnumerable<object>> _combinedTypes;

    public IEnumerable<object> CombinedTypes => _combinedTypes.Value;

    private object? _combinedType;

    public object? CombinedType
    {
        get => _combinedType;
        set => this.RaiseAndSetIfChanged(ref _combinedType, value);
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

    private void Generate()
    {
        if (Model.MicroController == null) return;
        switch (Model.EmulationType)
        {
            case EmulationType.Controller:
                if (CombinedType is SimpleType simpleType)
                {
                    switch (simpleType)
                    {
                        case Configuration.Types.SimpleType.WiiInputSimple:
                            Model.Bindings.Add(new WiiCombinedOutput(Model, Model.MicroController!));
                            break;
                        case Configuration.Types.SimpleType.Gh5NeckSimple:
                            Model.Bindings.Add(new Gh5CombinedOutput(Model, Model.MicroController!));
                            break;
                        case Configuration.Types.SimpleType.Ps2InputSimple:
                            Model.Bindings.Add(new Ps2CombinedOutput(Model, Model.MicroController!));
                            break;
                        case Configuration.Types.SimpleType.WtNeckSimple:
                            Model.Bindings.Add(new GhwtCombinedOutput(Model, Model.MicroController!));
                            break;
                        case Configuration.Types.SimpleType.DjTurntableSimple:
                            Model.Bindings.Add(new DjCombinedOutput(Model, Model.MicroController!));
                            break;
                    }
                }
                else if (CombinedType is StandardAxisType standardAxisType)
                {
                    Model.Bindings.Add(new ControllerAxis(Model, null, Colors.Transparent, Colors.Transparent, 1, 0, 0,
                        standardAxisType));
                }
                else if (CombinedType is StandardButtonType standardButtonType)
                {
                    Model.Bindings.Add(new ControllerButton(Model, null, Colors.Transparent, Colors.Transparent, 5,
                        standardButtonType));
                }

                break;
            case EmulationType.KeyboardMouse:
                if (MouseAxisType.HasValue)
                {
                    Model.Bindings.Add(new MouseAxis(Model, null, Colors.Transparent, Colors.Transparent, 1, 0, 0,
                        MouseAxisType.Value));
                }
                else if (MouseButtonType.HasValue)
                {
                    Model.Bindings.Add(new MouseButton(Model, null, Colors.Transparent, Colors.Transparent, 5,
                        MouseButtonType.Value));
                }
                else if (Key.HasValue)
                {
                    Model.Bindings.Add(new KeyboardButton(Model, null, Colors.Transparent, Colors.Transparent, 5,
                        Key.Value));
                }

                break;
        }

        Model.Bindings.Remove(this);
    }

    public override string? GetLocalisedName() => "Unset";
    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool RequiresInput => false;

    public override SerializedOutput GetJson()
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