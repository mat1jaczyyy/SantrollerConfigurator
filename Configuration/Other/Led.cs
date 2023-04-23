using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public enum StageKitCommand
{
    Fog,
    Strobe,
    LedGreen,
    LedRed,
    LedYellow,
    LedBlue,
}

public enum StageKitStrobeSpeed
{
    Slow,
    Medium,
    Fast,
    Fastest,
}

public enum FiveFretGuitar
{
    Open,
    Green,
    Red,
    Yellow,
    Blue,
    Orange
}

public enum SixFretGuitar
{
    Open,
    Black1,
    Black2,
    Black3,
    White1,
    White2,
    White3,
}

public enum RockBandDrum
{
    KickPedal,
    RedPad,
    YellowPad,
    BluePad,
    GreenPad,
    YellowCymbal,
    BlueCymbal,
    GreenCymbal
}

public enum GuitarHeroDrum
{
    KickPedal,
    RedPad,
    YellowCymbal,
    BluePad,
    OrangeCymbal,
    GreenPad
}

public enum Turntable
{
    GreenNoteBoth,
    RedNoteBoth,
    BlueNoteBoth,
    GreenNoteLeft,
    RedNoteLeft,
    BlueNoteLeft,
    GreenNoteRight,
    RedNoteRight,
    BlueNoteRight,
}

public enum LedCommandType
{
    [Description("Num Lock LED")] KeyboardNumLock,
    [Description("Caps Lock LED")] KeyboardCapsLock,
    [Description("Scroll Lock LED")] KeyboardScrollLock,

    [Description("Authenticated with console LED")]
    Auth,
    [Description("Player LEDs")] Player,
    [Description("Combo LEDs")] Combo,
    [Description("Input Reactive LEDs")] InputReactive,

    [Description("Star Power Percentage (When not active) LED")]
    StarPowerInactive,

    [Description("Star Power Percentage (When active) LED")]
    StarPowerActive,
    [Description("DJ Hero Euphoria LED")] DjEuphoria,

    [Description("Stage Kit")] StageKitLed,

    [Description("PS4 Light Bar")] Ps4LightBar
}

public enum RumbleCommand
{
    StageKitFogOn = 1,
    StageKitFogOff,
    StageKitStrobeLightSlow,
    StageKitStrobeLightMedium,
    StageKitStrobeLightFast,
    StageKitStrobeLightFastest,
    StageKitStrobeLightOff,
    SantrollerStarPowerGauge,
    SantrollerStarPowerActive,
    SantrollerMultiplier,
    SantrollerSolo,
    StageKitStrobeLightBlue = 0x20,
    StageKitStrobeLightGreen = 0x40,
    StageKitStrobeLightYellow = 0x60,
    StageKitStrobeLightRed = 0x80,
    SantrollerInputSpecific = 0x90,
    SantrollerEuphoriaLed = 0xA0,
    StageKitReset = 0xFF
}

public class Led : Output
{
    private readonly SourceList<LedCommandType> _rumbleCommands = new();
    private bool _outputEnabled;

    private int _pin;

    public Led(ConfigViewModel model, bool outputEnabled, int pin, Color ledOn,
        Color ledOff, byte[] ledIndices, LedCommandType command, int param, int param2) : base(model,
        new FixedInput(model, 0),
        ledOn, ledOff,
        ledIndices)
    {
        Player = 1;
        Combo = 1;
        StageKitLed = 1;
        FiveFretGuitar = 0;
        SixFretGuitar = 0;
        GuitarHeroDrum = 0;
        RockBandDrum = 0;
        Turntable = 0;
        StageKitCommand = 0;
        StrobeSpeed = 0;
        switch (command)
        {
            case LedCommandType.InputReactive:
                switch (model.DeviceType)
                {
                    case DeviceControllerType.Guitar:
                        FiveFretGuitar = (FiveFretGuitar) param;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        SixFretGuitar = (SixFretGuitar) param;
                        break;
                    case DeviceControllerType.Drum when Model.RhythmType is RhythmType.GuitarHero:
                        GuitarHeroDrum = (GuitarHeroDrum) param;
                        break;
                    case DeviceControllerType.Drum when Model.RhythmType is RhythmType.RockBand:
                        RockBandDrum = (RockBandDrum) param;
                        break;
                    case DeviceControllerType.Turntable:
                        Turntable = (Turntable) param;
                        break;
                }

                break;
            case LedCommandType.Player:
                Player = param + 1;
                break;
            case LedCommandType.Combo:
                Combo = param + 1;
                break;
            case LedCommandType.StageKitLed:
                StageKitCommand = (StageKitCommand) param;
                switch (StageKitCommand)
                {
                    case StageKitCommand.Strobe:
                        StrobeSpeed = (StageKitStrobeSpeed) param2;
                        break;
                    default:
                        StageKitLed = param2 + 1;
                        break;
                }

                break;
        }

        Pin = pin;
        OutputEnabled = outputEnabled;
        Command = command;
        _rumbleCommands.AddRange(Enum.GetValues<LedCommandType>());
        _rumbleCommands.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.EmulationType, x => x.Model.RhythmType,
                x => x.Model.IsApa102).Select(FilterLeds))
            .Bind(out var rumbleCommands)
            .Subscribe();
        RumbleCommands = rumbleCommands;
        _ledsRequireColours = this.WhenAnyValue(x => x.Command).Select(s => s is not LedCommandType.Ps4LightBar)
            .ToProperty(this, x => x.LedsRequireColours);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceType)
            .Select(s => s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.Guitar)
            .ToPropertyEx(this, x => x.FiveFretMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceType)
            .Select(s => s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.LiveGuitar)
            .ToPropertyEx(this, x => x.SixFretMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceType, x => x.Model.RhythmType)
            .Select(s =>
                s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.Drum &&
                s.Item3 is RhythmType.GuitarHero)
            .ToPropertyEx(this, x => x.GuitarHeroDrumsMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceType, x => x.Model.RhythmType)
            .Select(s =>
                s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.Drum &&
                s.Item3 is RhythmType.RockBand)
            .ToPropertyEx(this, x => x.RockBandDrumsMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceType)
            .Select(s => s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.Turntable)
            .ToPropertyEx(this, x => x.TurntableMode);

        this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.Player)
            .ToPropertyEx(this, x => x.PlayerMode);

        this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.Combo)
            .ToPropertyEx(this, x => x.ComboMode);

        this.WhenAnyValue(x => x.Command, x => x.StageKitCommand).Select(s =>
                s.Item1 is LedCommandType.StageKitLed && s.Item2 is StageKitCommand.Strobe)
            .ToPropertyEx(this, x => x.StageKitStrobeSpeedMode);

        this.WhenAnyValue(x => x.Command, x => x.StageKitCommand).Select(s =>
                s.Item1 is LedCommandType.StageKitLed && s.Item2 is StageKitCommand.LedBlue or StageKitCommand.LedGreen
                    or StageKitCommand.LedRed or StageKitCommand.LedYellow)
            .ToPropertyEx(this, x => x.StageKitLedMode);
        
        this.WhenAnyValue(x => x.Command).Select(s => s is LedCommandType.StageKitLed)
            .ToPropertyEx(this, x => x.StageKitMode);
        UpdateDetails();
    }

    private ObservableAsPropertyHelper<bool> _ledsRequireColours;
    public override bool LedsRequireColours => _ledsRequireColours.Value;
    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool FiveFretMode { get; }
    [ObservableAsProperty] public bool SixFretMode { get; }
    [ObservableAsProperty] public bool GuitarHeroDrumsMode { get; }
    [ObservableAsProperty] public bool RockBandDrumsMode { get; }
    [ObservableAsProperty] public bool TurntableMode { get; }
    [ObservableAsProperty] public bool PlayerMode { get; }
    [ObservableAsProperty] public bool ComboMode { get; }
    [ObservableAsProperty] public bool StageKitStrobeSpeedMode { get; }
    [ObservableAsProperty] public bool StageKitLedMode { get; }
    [ObservableAsProperty] public bool StageKitMode { get; }

    // ReSharper enable UnassignedGetOnlyAutoProperty
    public StageKitCommand[] StageKitCommands { get; } = Enum.GetValues<StageKitCommand>();
    public StageKitStrobeSpeed[] StageKitStrobeSpeeds { get; } = Enum.GetValues<StageKitStrobeSpeed>();
    public FiveFretGuitar[] FiveFretGuitars { get; } = Enum.GetValues<FiveFretGuitar>();
    public SixFretGuitar[] SixFretGuitars { get; } = Enum.GetValues<SixFretGuitar>();
    public RockBandDrum[] RockBandDrums { get; } = Enum.GetValues<RockBandDrum>();
    public GuitarHeroDrum[] GuitarHeroDrums { get; } = Enum.GetValues<GuitarHeroDrum>();
    public Turntable[] Turntables { get; } = Enum.GetValues<Turntable>();

    public bool OutputEnabled
    {
        get => _outputEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputEnabled, value);
            if (value)
            {
                if (PinConfig != null) return;
                PinConfig = Model.Microcontroller.GetOrSetPin(Model, "led", Pin, DevicePinMode.Output);
                Model.Microcontroller.AssignPin(PinConfig);
            }
            else
            {
                Dispose();
            }

            Model.UpdateErrors();
        }
    }

    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(false);

    public DirectPinConfig? PinConfig { get; private set; }

    public int Pin
    {
        get => _pin;
        set
        {
            this.RaiseAndSetIfChanged(ref _pin, value);
            if (PinConfig == null) return;
            PinConfig.Pin = value;
        }
    }

    private LedCommandType _command;
    public LedCommandType Command
    {
        get=>_command;
        set
        {
            this.RaiseAndSetIfChanged(ref _command, value);
            UpdateDetails();
        }
    }

    [Reactive] public int Player { get; set; }
    [Reactive] public StageKitStrobeSpeed StrobeSpeed { get; set; }
    [Reactive] public int StageKitLed { get; set; }

    [Reactive] public int Combo { get; set; }

    [Reactive] public GuitarHeroDrum GuitarHeroDrum { get; set; }
    [Reactive] public RockBandDrum RockBandDrum { get; set; }
    [Reactive] public Turntable Turntable { get; set; }
    [Reactive] public FiveFretGuitar FiveFretGuitar { get; set; }
    [Reactive] public SixFretGuitar SixFretGuitar { get; set; }

    [Reactive] public StageKitCommand StageKitCommand { get; set; }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public ReadOnlyObservableCollection<LedCommandType> RumbleCommands { get; }


    public override string LedOnLabel => Command switch
    {
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => "Fog Active LED Colour",
        LedCommandType.Player or LedCommandType.Auth => "LED Colour",
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => "Start Power Full Colour",
        _ => "Active LED Colour"
    };

    public override string LedOffLabel => Command switch
    {
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => "Fog Inactive LED Colour",
        LedCommandType.Player or LedCommandType.Auth => "LED Colour",
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => "Start Power Empty Colour",
        _ => "Inactive LED Colour"
    };

    public override bool SupportsLedOff => Command is not LedCommandType.Auth or LedCommandType.Player;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;

    public override bool Valid => true;

    public override void Dispose()
    {
        if (PinConfig == null) return;
        Model.Microcontroller.UnAssignPins(PinConfig.Type);
        PinConfig = null;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Led Command - " + EnumToStringConverter.Convert(Command);
    }

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return PinConfig != null ? new[] {PinConfig} : Enumerable.Empty<PinConfig>();
    }

    protected override IEnumerable<DevicePin> GetOwnPins()
    {
        return new List<DevicePin>
        {
            new(Pin, DevicePinMode.Output)
        };
    }

    public static Func<LedCommandType, bool> FilterLeds(
        (DeviceControllerType controllerType, EmulationType emulationType, RhythmType rhythmType, bool isApa102) type)
    {
        return command =>
        {
            return type.emulationType switch
            {
                EmulationType.KeyboardMouse or EmulationType.BluetoothKeyboardMouse => command is
                    LedCommandType.KeyboardCapsLock or LedCommandType.KeyboardScrollLock
                    or LedCommandType.KeyboardNumLock,
                EmulationType.Controller or EmulationType.Bluetooth => command switch
                {
                    LedCommandType.Auth or LedCommandType.Player => true,
                    LedCommandType.Combo or LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive
                        or LedCommandType.InputReactive or LedCommandType.StageKitLed when
                        type.controllerType is DeviceControllerType.Drum or DeviceControllerType.Guitar
                            or DeviceControllerType.LiveGuitar or DeviceControllerType.Turntable
                            or DeviceControllerType.StageKit => true,
                    LedCommandType.DjEuphoria when type.controllerType is DeviceControllerType.Turntable => true,
                    _ => false
                },
                _ => false
            };
        };
    }

    public override SerializedOutput Serialize()
    {
        var param1 = 0;
        var param2 = 0;
        switch (Command)
        {
            case LedCommandType.InputReactive:
                switch (Model.DeviceType)
                {
                    case DeviceControllerType.Guitar:
                        param1 = (int) FiveFretGuitar;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        param1 = (int) SixFretGuitar;
                        break;
                    case DeviceControllerType.Drum when Model.RhythmType is RhythmType.GuitarHero:
                        param1 = (int) GuitarHeroDrum;
                        break;
                    case DeviceControllerType.Drum when Model.RhythmType is RhythmType.RockBand:
                        param1 = (int) RockBandDrum;
                        break;
                    case DeviceControllerType.Turntable:
                        param1 = (int) Turntable;
                        break;
                }

                break;
            case LedCommandType.Player:
                param1 = Player - 1;
                break;
            case LedCommandType.Combo:
                param1 = Combo - 1;
                break;
            case LedCommandType.StageKitLed:
                param1 = (int) StageKitCommand;
                param2 = StageKitCommand switch
                {
                    StageKitCommand.Strobe => (int) StrobeSpeed,
                    _ => StageKitLed - 1
                };

                break;
        }

        return new SerializedLed(LedOn, LedOff, LedIndices.ToArray(), Command, param1, param2, OutputEnabled, Pin);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"Led/{Command}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce)
    {
        if (mode is not (ConfigField.StrobeLed or ConfigField.AuthLed or ConfigField.PlayerLed or ConfigField.RumbleLed
            or ConfigField.KeyboardLed or ConfigField.LightBarLed or ConfigField.OffLed)) return "";
        var on = "";
        var off = "";
        if (PinConfig != null)
        {
            on = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, true) + ";";
            off = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, false) + ";";
        }

        var between = "";
        var starPowerBetween = "";
        foreach (var index in LedIndices)
        {
            on += $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, index)}";
            off += $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, index)}";
            between +=
                $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, LedOff, "rumble_left", index)}";
            starPowerBetween +=
                $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, LedOff, "last_start_power", index)}";
        }

        switch (Command)
        {
            case LedCommandType.Ps4LightBar when mode is ConfigField.LightBarLed:
                return string.Join("\n",
                    LedIndices.Select(index => Model.LedType.GetLedAssignment("red", "green", "blue", index)));
            // Player and Auth commands are a set and forget type thing, they are never switched off after being turned on
            case LedCommandType.Player when mode is ConfigField.PlayerLed:
                return $@"
                if (player == {Player}) {{
                    {on}
                }}";
            case LedCommandType.Auth when mode is ConfigField.AuthLed:
                return on;
        }

        if (mode is ConfigField.KeyboardLed && Command is LedCommandType.KeyboardCapsLock
                or LedCommandType.KeyboardNumLock
                or LedCommandType.KeyboardScrollLock)
        {
            return
                $@"if (leds & {1 << (Command - LedCommandType.KeyboardCapsLock)}) {{
                    {on}
                }} else {{
                    {off}
                }}";
        }

        switch (Command)
        {
            case LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Strobe &&
                                                 mode == ConfigField.StrobeLed:
                return @$"if (last_strobe && last_strobe - millis() > stage_kit_millis[strobe_delay]) {{
                        last_strobe = millis();
                        {on}
                    }} else if (last_strobe && last_strobe - millis() > 10) {{
                        {off}
                    }}";
            case LedCommandType.StarPowerActive when
                StageKitCommand is StageKitCommand.Strobe && mode == ConfigField.StrobeLed:
                return $@"if (star_power_active) {{
                             {starPowerBetween}
                          }}";
            case LedCommandType.StarPowerInactive when
                StageKitCommand is StageKitCommand.Strobe && mode == ConfigField.StrobeLed:
                return $@"if (!star_power_active) {{
                             {starPowerBetween}
                          }}";
            case LedCommandType.StarPowerInactive or LedCommandType.StarPowerActive when
                StageKitCommand is StageKitCommand.Strobe && mode == ConfigField.RumbleLed:
                return
                    $@"if (rumble_right == {RumbleCommand.SantrollerStarPowerGauge} && rumble_left != {RumbleCommand.SantrollerStarPowerGauge}) {{
                           last_star_power = rumble_left;
                      }}";
            case LedCommandType.StarPowerInactive when
                StageKitCommand is StageKitCommand.Strobe && mode == ConfigField.RumbleLed:
                return $@"if (rumble_right == {RumbleCommand.SantrollerStarPowerActive}) {{
                           if (rumble_left && !star_power_active) {{
                                {off}
                                star_power_active = true;
                           }}
                      }}";
            case LedCommandType.StarPowerActive when
                StageKitCommand is StageKitCommand.Strobe && mode == ConfigField.RumbleLed:
                return $@"if (rumble_right == {RumbleCommand.SantrollerStarPowerActive}) {{
                           if (!rumble_left && star_power_active) {{
                                {off}
                                star_power_active = false;
                           }}
                      }}";
        }

        if (mode is ConfigField.OffLed && Command is LedCommandType.StageKitLed)
        {
            return off;
        }
        if (mode is not ConfigField.RumbleLed) return "";


        switch (Command)
        {
            case LedCommandType.DjEuphoria:
                return $@"if (rumble_left != rumble_right)) {{
                {between}
            }}";
            case LedCommandType.Combo:
                return $@"if (rumble_right == {(int) RumbleCommand.SantrollerMultiplier}) {{
                    if (rumble_left == {Combo + 10}) {{
                        {on}
                    }} else {{
                        {off}
                    }}
                }}";
            case LedCommandType.InputReactive:
            {
                var santrollerCmd = Model.DeviceType switch
                {
                    DeviceControllerType.Guitar => (int) FiveFretGuitar,
                    DeviceControllerType.LiveGuitar => (int) SixFretGuitar,
                    DeviceControllerType.Drum when Model.RhythmType is RhythmType.GuitarHero => (int) GuitarHeroDrum,
                    DeviceControllerType.Drum when Model.RhythmType is RhythmType.RockBand => (int) RockBandDrum,
                    DeviceControllerType.Turntable => (int) Turntable,
                    _ => 0
                };
                return $@"if (rumble_right == {(int) (RumbleCommand.SantrollerInputSpecific + santrollerCmd)}) {{
                    if (rumble_left == 1) {{
                        {on}
                    }} else {{
                        {off}
                    }}
                }}";
            }
        }


        if (Command is not LedCommandType.StageKitLed) return "";
        switch (StageKitCommand)
        {
            case StageKitCommand.Fog:
                return $@"if ((rumble_left == 0 && rumble_right == {RumbleCommand.StageKitFogOff})) {{
                          {off}
                      }} else if (rumble_left == 0 && rumble_right == {RumbleCommand.StageKitFogOn}) {{
                          {on}
                      }}";
            case StageKitCommand.Strobe:
                return
                    $@"if (rumble_left == 0 && rumble_right >= {RumbleCommand.StageKitStrobeLightSlow} && rumble_right <= {RumbleCommand.StageKitStrobeLightFastest}) {{
                           strobe_delay = 5 - (rumble_right - {RumbleCommand.StageKitFogOff});
                      }}
                      if (strobe_delay == 0) {{
                          strobe_delay = 0;
                          {off}
                      }}";
            case StageKitCommand.LedBlue:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    @$"if ((rumble_left & {led} == 0) && (rumble_right == {RumbleCommand.StageKitStrobeLightBlue})) {{
                          {off}
                      }} else if (rumble_left & ({led}) && rumble_right == {RumbleCommand.StageKitStrobeLightBlue}) {{
                          {on}
                      }}";
            }
            case StageKitCommand.LedGreen:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    @$"if ((rumble_left & {led} == 0) && (rumble_right == {RumbleCommand.StageKitStrobeLightGreen}) {{
                          {off}
                      }} else if (rumble_left & ({led}) && rumble_right == {RumbleCommand.StageKitStrobeLightGreen}) {{
                          {on}
                      }}";
            }
            case StageKitCommand.LedRed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    @$"if ((rumble_left & {led} == 0) && (rumble_right == {RumbleCommand.StageKitStrobeLightRed}) {{
                          {off}
                      }} else if (rumble_left & ({led}) && rumble_right == {RumbleCommand.StageKitStrobeLightRed}) {{
                          {on}
                      }}";
            }
            case StageKitCommand.LedYellow:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    @$"if ((rumble_left & {led} == 0) && (rumble_right == {RumbleCommand.StageKitStrobeLightYellow}) {{
                          {off}
                      }} else if (rumble_left & ({led}) && rumble_right == {RumbleCommand.StageKitStrobeLightYellow}) {{
                          {on}
                      }}";
            }
            default:
                return "";
        }
    }

    public override void UpdateBindings()
    {
    }
}