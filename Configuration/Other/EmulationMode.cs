using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class EmulationMode : Output
{
    private readonly SourceList<EmulationModeType> _emulationModes = new();
    private EmulationModeType _emulationModeType;

    public EmulationMode(ConfigViewModel model, Input input, EmulationModeType type) : base(
        model, input, Colors.Black, Colors.Black, Array.Empty<byte>(), false)
    {
        Type = type;
        _emulationModes.AddRange(Enum.GetValues<EmulationModeType>());
        _emulationModes.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.RhythmType, x => x.Model.DeviceType).Select(CreateFilter))
            .Bind(out var modes)
            .Subscribe();
        EmulationModes = modes;
    }

    public ReadOnlyObservableCollection<EmulationModeType> EmulationModes { get; }

    public EmulationModeType Type
    {
        get => _emulationModeType;
        set
        {
            this.RaiseAndSetIfChanged(ref _emulationModeType, value);
            UpdateDetails();
        }
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    private static Func<EmulationModeType, bool> CreateFilter(
        (RhythmType rhythmType, DeviceControllerType deviceControllerType) tuple)
    {
        return mode => mode != EmulationModeType.Wii ||
                       (tuple.deviceControllerType is DeviceControllerType.Drum or DeviceControllerType.Guitar &&
                        tuple.rhythmType == RhythmType.RockBand);
    }

    private string GetDefinition()
    {
        return Type switch
        {
            EmulationModeType.Xbox360 => "XBOX360",
            EmulationModeType.XboxOne => "XBOXONE",
            EmulationModeType.Wii => "WII_RB",
            EmulationModeType.Ps3 => "REAL_PS3",
            EmulationModeType.Ps4Or5 => "PS4",
            EmulationModeType.Switch => "SWITCH",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedEmulationMode(Type, Input.Serialise());
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type) + " Console Mode Binding";
    }

    public override object GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        return mode != ConfigField.Detection ? "" : $@"
            if ({Input.Generate()}) {{
                set_console_type({GetDefinition()});
            }}";
    }

    public override void UpdateBindings()
    {
    }
}