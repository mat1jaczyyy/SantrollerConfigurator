using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    LedBlue
}

public enum StageKitStrobeSpeed
{
    Slow,
    Medium,
    Fast,
    Fastest
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
    White3
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
    ScratchLeft,
    GreenNoteLeft,
    RedNoteLeft,
    BlueNoteLeft,
    ScratchRight,
    GreenNoteRight,
    RedNoteRight,
    BlueNoteRight
}

public enum LedCommandType
{
    KeyboardNumLock,
    KeyboardCapsLock,
    KeyboardScrollLock,
    Auth,
    Player,
    Combo,
    InputReactive,
    StarPowerInactive,
    StarPowerActive,
    DjEuphoria,
    StageKitLed,
    Ps4LightBar
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

    private readonly ObservableAsPropertyHelper<bool> _ledsRequireColours;
    private bool _outputEnabled;

    private int _pin;

    public Led(ConfigViewModel model, bool outputEnabled, bool inverted, int pin, Color ledOn,
        Color ledOff, byte[] ledIndices, LedCommandType command, int param, int param2) : base(model,
        new FixedInput(model, 0, false),
        ledOn, ledOff,
        ledIndices, false)
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
                switch (model.DeviceControllerType)
                {
                    case DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar:
                        FiveFretGuitar = (FiveFretGuitar) param;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        SixFretGuitar = (SixFretGuitar) param;
                        break;
                    case DeviceControllerType.GuitarHeroDrums:
                        GuitarHeroDrum = (GuitarHeroDrum) param;
                        break;
                    case DeviceControllerType.RockBandDrums:
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
        Inverted = inverted;
        Command = command;
        _rumbleCommands.AddRange(Enum.GetValues<LedCommandType>());
        _rumbleCommands.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceControllerType, x => x.Model.EmulationType,
                x => x.Model.IsApa102).Select(FilterLeds))
            .Bind(out var rumbleCommands)
            .Subscribe();
        RumbleCommands = rumbleCommands;
        _ledsRequireColours = this.WhenAnyValue(x => x.Command).Select(s => s is not LedCommandType.Ps4LightBar)
            .ToProperty(this, x => x.LedsRequireColours);
        this.WhenAnyValue(x => x.Command)
            .Select(commandType => commandType is LedCommandType.DjEuphoria
                or LedCommandType.StarPowerActive
                or LedCommandType.StarPowerInactive).ToPropertyEx(this, x => x.UsesPwm);
        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s => s.Item1 is LedCommandType.InputReactive && s.Item2.Is5FretGuitar())
            .ToPropertyEx(this, x => x.FiveFretMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s => s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.LiveGuitar)
            .ToPropertyEx(this, x => x.SixFretMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s =>
                s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.GuitarHeroDrums)
            .ToPropertyEx(this, x => x.GuitarHeroDrumsMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
            .Select(s =>
                s.Item1 is LedCommandType.InputReactive && s.Item2 is DeviceControllerType.RockBandDrums)
            .ToPropertyEx(this, x => x.RockBandDrumsMode);

        this.WhenAnyValue(x => x.Command, x => x.Model.DeviceControllerType)
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

    public override bool LedsRequireColours =>
        _ledsRequireColours.Value; // ReSharper disable UnassignedGetOnlyAutoProperty

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
                PinConfig = new DirectPinConfig(Model, "led", Pin, DevicePinMode.Output);
            }
            else
            {
                PinConfig = null;
            }

            Model.UpdateErrors();
        }
    }

    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(false);
    public List<int> AvailablePwmPins => Model.Microcontroller.PwmPins;

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
        get => _command;
        set
        {
            this.RaiseAndSetIfChanged(ref _command, value);
            if (UsesPwm && !AvailablePwmPins.Contains(_pin))
            {
                _pin = -1;
            }

            UpdateDetails();
        }
    }

    [Reactive] public bool Inverted { get; set; }

    [ObservableAsProperty] public bool UsesPwm { get; }
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
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => Resources.LEDColourActiveFog,
        LedCommandType.Player or LedCommandType.Auth => Resources.LedColourActive,
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => Resources.LedColourActiveStarPower,
        LedCommandType.DjEuphoria => Resources.LedColourActiveDjEuphoria,
        _ => Resources.LedColourActive
    };

    public override string LedOffLabel => Command switch
    {
        LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Fog => Resources.LedColourInactiveFog,
        LedCommandType.Player or LedCommandType.Auth => Resources.LedColourInactive,
        LedCommandType.StarPowerActive or LedCommandType.StarPowerInactive => Resources.LedColourInactiveStarPower,
        LedCommandType.DjEuphoria => Resources.LedColourInactiveDjEuphoria,
        _ => Resources.LedColourInactive
    };

    public override bool SupportsLedOff => Command is not (LedCommandType.Auth or LedCommandType.Player);

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return string.Format(Resources.LedCommandTitle, EnumToStringConverter.Convert(Command));
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
        (DeviceControllerType controllerType, EmulationType emulationType, bool isApa102) type)
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
                        type.controllerType is DeviceControllerType.RockBandDrums
                            or DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandGuitar
                            or DeviceControllerType.RockBandGuitar
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
                switch (Model.DeviceControllerType)
                {
                    case DeviceControllerType.GuitarHeroGuitar:
                    case DeviceControllerType.RockBandGuitar:
                        param1 = (int) FiveFretGuitar;
                        break;
                    case DeviceControllerType.LiveGuitar:
                        param1 = (int) SixFretGuitar;
                        break;
                    case DeviceControllerType.GuitarHeroDrums:
                        param1 = (int) GuitarHeroDrum;
                        break;
                    case DeviceControllerType.RockBandDrums:
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

        return new SerializedLed(LedOn, LedOff, LedIndices.ToArray(), Command, param1, param2, OutputEnabled, Inverted,
            Pin);
    }

    public override Enum GetOutputType()
    {
        return Command;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode is not (ConfigField.StrobeLed or ConfigField.AuthLed or ConfigField.PlayerLed or ConfigField.RumbleLed
            or ConfigField.KeyboardLed or ConfigField.LightBarLed or ConfigField.OffLed or ConfigField.InitLed)) return "";
        var on = "";
        var off = "";
        var between = "";
        var starPowerBetween = "";
        if (PinConfig != null)
        {
            on = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, !Inverted) + ";";
            off = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, Inverted) + ";";
            between =
                Model.Microcontroller.GenerateAnalogWrite(PinConfig.Pin, (Inverted ? "(255-" : "(") + "rumble_left)") +
                ";";
            starPowerBetween =
                Model.Microcontroller.GenerateAnalogWrite(PinConfig.Pin,
                    (Inverted ? "(255-" : "(") + "last_star_power)") + ";";
        }

        foreach (var index in LedIndices)
        {
            on += $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, index)}";
            off += $@"ledState[{index - 1}].select = 0;{Model.LedType.GetLedAssignment(LedOff, index)}";
            between +=
                $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(index, LedOn, LedOff, "rumble_left")}";
            starPowerBetween +=
                $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(index, LedOn, LedOff, "last_star_power")}";
        }

        switch (Command)
        {
            case LedCommandType.Ps4LightBar when mode is ConfigField.LightBarLed:
                return string.Join("\n",
                    LedIndices.Select(index => Model.LedType.GetLedAssignment("red", "green", "blue", index)));
            // Player and Auth commands are a set and forget type thing, they are never switched off after being turned on
            case LedCommandType.Player when mode is ConfigField.PlayerLed:
                return $$"""
                         if (player == {{Player}}) {
                             {{on}}
                         }
                         """;
            case LedCommandType.Auth when mode is ConfigField.AuthLed:
                return on;
        }

        switch (mode)
        {
            case ConfigField.InitLed:
                return off;
            case ConfigField.KeyboardLed when Command is LedCommandType.KeyboardCapsLock
                or LedCommandType.KeyboardNumLock
                or LedCommandType.KeyboardScrollLock:
                return
                    $$"""
                      if (leds & {{1 << (Command - LedCommandType.KeyboardCapsLock)}}) {
                          {{on}}
                      } else {
                          {{off}}
                      }
                      """;
        }

        switch (Command)
        {
            case LedCommandType.StageKitLed when StageKitCommand is StageKitCommand.Strobe &&
                                                 mode == ConfigField.StrobeLed:
                return $$"""
                         if (last_strobe && last_strobe - millis() > stage_kit_millis[strobe_delay]) {
                         last_strobe = millis();
                             {{on}}
                         } else if (last_strobe && last_strobe - millis() > 10) {
                             {{off}}
                         }
                         """;
            case LedCommandType.StarPowerInactive when mode == ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{RumbleCommand.SantrollerStarPowerActive}} && !rumble_left) {
                              star_power_active = false;
                              {{starPowerBetween}}
                         }
                         if (!star_power_active && rumble_right == {{RumbleCommand.SantrollerStarPowerGauge}}) {
                              last_star_power = rumble_left;
                              {{starPowerBetween}}
                         }
                         """;
            case LedCommandType.StarPowerActive when mode == ConfigField.RumbleLed:
                return $$"""
                         if (rumble_right == {{RumbleCommand.SantrollerStarPowerActive}} && rumble_left) {
                              star_power_active = true;
                              {{starPowerBetween}}
                         }
                         if (star_power_active && rumble_right == {{RumbleCommand.SantrollerStarPowerGauge}}) {
                              last_star_power = rumble_left;
                              {{starPowerBetween}}
                         }
                         """;
        }

        if (mode is ConfigField.OffLed && Command is LedCommandType.StageKitLed) return off;
        if (mode is not ConfigField.RumbleLed) return "";


        switch (Command)
        {
            case LedCommandType.DjEuphoria:
                return $$"""
                         if (rumble_left != rumble_right) {
                             {{between}}
                         }
                         """;
            case LedCommandType.Combo:
                return $$"""
                         if (rumble_right == {{(int) RumbleCommand.SantrollerMultiplier}}) {
                             if (rumble_left == {{Combo + 10}}) {
                                 {{on}}
                             } else if (rumble_left == 0) {
                                 {{off}}
                             }
                         }
                         """;
            case LedCommandType.InputReactive:
            {
                var santrollerCmd = Model.DeviceControllerType switch
                {
                    DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar =>
                        (int) FiveFretGuitar,
                    DeviceControllerType.LiveGuitar => (int) SixFretGuitar,
                    DeviceControllerType.GuitarHeroDrums => (int) GuitarHeroDrum,
                    DeviceControllerType.RockBandDrums => (int) RockBandDrum,
                    DeviceControllerType.Turntable => (int) Turntable,
                    _ => 0
                };

                return $$"""
                         if (rumble_right == {{(int) (RumbleCommand.SantrollerInputSpecific + santrollerCmd)}}) {
                             if (rumble_left == 1) {
                                 {{on}}
                             } else {
                                 {{off}}
                             }
                         }
                         """;
            }
        }


        if (Command is not LedCommandType.StageKitLed) return "";
        switch (StageKitCommand)
        {
            case StageKitCommand.Fog:
                return $$"""
                         if ((rumble_left == 0 && rumble_right == {{RumbleCommand.StageKitFogOff}})) {
                             {{off}}
                         } else if (rumble_left == 0 && rumble_right == {{RumbleCommand.StageKitFogOn}}) {
                             {{on}}
                         }
                         """;
            case StageKitCommand.Strobe:
                return
                    $$"""
                      if (rumble_left == 0 && rumble_right >= {{RumbleCommand.StageKitStrobeLightSlow}} && rumble_right <= {{RumbleCommand.StageKitStrobeLightFastest}}) {
                           strobe_delay = 5 - (rumble_right - {{RumbleCommand.StageKitFogOff}});
                      }
                      if (strobe_delay == 0) {
                          strobe_delay = 0;
                          {{off}}
                      }
                      """;
            case StageKitCommand.LedBlue:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((rumble_left & {{led}} == 0) && (rumble_right == {{RumbleCommand.StageKitStrobeLightBlue}})) {
                          {{off}}
                      } else if (rumble_left & ({{led}}) && rumble_right == {{RumbleCommand.StageKitStrobeLightBlue}}) {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedGreen:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((rumble_left & {{led}} == 0) && (rumble_right == {{RumbleCommand.StageKitStrobeLightGreen}}) {
                          {{off}}
                      } else if (rumble_left & ({{led}}) && rumble_right == {{RumbleCommand.StageKitStrobeLightGreen}}) {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedRed:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((rumble_left & {{led}} == 0) && (rumble_right == {{RumbleCommand.StageKitStrobeLightRed}}) {
                          {{off}}
                      } else if (rumble_left & ({{led}}) && rumble_right == {{RumbleCommand.StageKitStrobeLightRed}}) {
                          {{on}}
                      }
                      """;
            }
            case StageKitCommand.LedYellow:
            {
                var led = 1 << (StageKitLed - 1);
                return
                    $$"""
                      if ((rumble_left & {{led}} == 0) && (rumble_right == {{RumbleCommand.StageKitStrobeLightYellow}}) {
                          {{off}}
                      } else if (rumble_left & ({{led}}) && rumble_right == {{RumbleCommand.StageKitStrobeLightYellow}}) {
                          {{on}}
                      }
                      """;
            }
            default:
                return "";
        }
    }

    public override void UpdateBindings()
    {
    }
}