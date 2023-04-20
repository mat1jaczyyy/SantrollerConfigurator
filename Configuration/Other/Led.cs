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

namespace GuitarConfigurator.NetCore.Configuration.Other;

public enum StageKitFogCommands
{
    On = 1,
    Off
}

public enum RumbleCommand
{
    [Description("Authenticated with console LED")]
    Auth,
    [Description("First Player LED")] Player1,
    [Description("Second Player LED")] Player2,
    [Description("Third Player LED")] Player3,
    [Description("Fourth Player LED")] Player4,
    [Description("Num Lock LED")] KeyboardNumLock,
    [Description("Caps Lock LED")] KeyboardCapsLock,
    [Description("Scroll Lock LED")] KeyboardScrollLock,
    [Description("Stage Kit Fog")] StageKitFog,
    [Description("Combo LED")] SantrollerCombo = 10,
    [Description("Solo LED")] SantrollerSolo,
    [Description("Open Note LED")] SantrollerOpen,
    [Description("Green LED")] SantrollerGreen,
    [Description("Red LED")] SantrollerRed,
    [Description("Yellow LED")] SantrollerYellow,
    [Description("Blue LED")] SantrollerBlue,
    [Description("Orange LED")] SantrollerOrange,
    [Description("Blue Cymbal LED")] SantrollerCymbalBlue,
    [Description("Yellow Cymbal LED")] SantrollerCymbalYellow,
    [Description("Green Cymbal LED")] SantrollerCymbalGreen,
    [Description("Kick LED")] SantrollerKick,

    [Description("Star Power Gauge (Inactive) LED")]
    SantrollerStarPowerFill,

    [Description("Star Power Gauge (Active) LED")]
    SantrollerStarPowerActive,
    [Description("DJ Hero Euphoria LED")] DjEuphoria,

    [Description("Stage Kit Strobe Light")]
    StageKitStrobe,

    [Description("Stage Kit Green Led 1")]
    StageKitGreen1 = 0x40,

    [Description("Stage Kit Green Led 2")]
    StageKitGreen2,

    [Description("Stage Kit Green Led 3")]
    StageKitGreen3,

    [Description("Stage Kit Green Led 4")]
    StageKitGreen4,

    [Description("Stage Kit Green Led 5")]
    StageKitGreen5,

    [Description("Stage Kit Green Led 6")]
    StageKitGreen6,

    [Description("Stage Kit Green Led 7")]
    StageKitGreen7,

    [Description("Stage Kit Green Led 8")]
    StageKitGreen8,

    [Description("Stage Kit Red Led 1")]
    StageKitRed1 = 0x80,

    [Description("Stage Kit Red Led 2")]
    StageKitRed2,

    [Description("Stage Kit Red Led 3")]
    StageKitRed3,

    [Description("Stage Kit Red Led 4")]
    StageKitRed4,

    [Description("Stage Kit Red Led 5")]
    StageKitRed5,

    [Description("Stage Kit Red Led 6")]
    StageKitRed6,

    [Description("Stage Kit Red Led 7")]
    StageKitRed7,

    [Description("Stage Kit Red Led 8")]
    StageKitRed8,

    [Description("Stage Kit Yellow Led 1")]
    StageKitYellow1 = 0x60,

    [Description("Stage Kit Yellow Led 2")]
    StageKitYellow2,

    [Description("Stage Kit Yellow Led 3")]
    StageKitYellow3,

    [Description("Stage Kit Yellow Led 4")]
    StageKitYellow4,

    [Description("Stage Kit Yellow Led 5")]
    StageKitYellow5,

    [Description("Stage Kit Yellow Led 6")]
    StageKitYellow6,

    [Description("Stage Kit Yellow Led 7")]
    StageKitYellow7,

    [Description("Stage Kit Yellow Led 8")]
    StageKitYellow8,

    [Description("Stage Kit Blue Led 1")]
    StageKitBlue1 = 0x20,

    [Description("Stage Kit Blue Led 2")]
    StageKitBlue2,

    [Description("Stage Kit Blue Led 3")]
    StageKitBlue3,

    [Description("Stage Kit Blue Led 4")]
    StageKitBlue4,

    [Description("Stage Kit Blue Led 5")]
    StageKitBlue5,

    [Description("Stage Kit Blue Led 6")]
    StageKitBlue6,

    [Description("Stage Kit Blue Led 7")]
    StageKitBlue7,

    [Description("Stage Kit Blue Led 8")]
    StageKitBlue8,

    [Description("PS4 Light Bar")] Ps4LightBar
}

public static class RumbleCommandMethods
{
    public static bool IsStageKit(this RumbleCommand command)
    {
        return command.ToString().StartsWith("StageKit");
    }

    public static bool IsSantroller(this RumbleCommand command)
    {
        return command.ToString().StartsWith("Santroller");
    }

    public static bool IsPlayer(this RumbleCommand command)
    {
        return command.ToString().StartsWith("Player");
    }

    public static bool IsKeyboard(this RumbleCommand command)
    {
        return command.ToString().StartsWith("Keyboard");
    }

    public static bool IsDj(this RumbleCommand command)
    {
        return command is RumbleCommand.DjEuphoria;
    }
    
    public static bool IsDrum(this RumbleCommand command)
    {
        return command is RumbleCommand.SantrollerCymbalBlue or RumbleCommand.SantrollerCymbalYellow or RumbleCommand.SantrollerCymbalBlue or RumbleCommand.SantrollerKick;
    }

    public static bool IsAuth(this RumbleCommand command)
    {
        return command is RumbleCommand.Auth;
    }
}

public class Led : Output
{
    private readonly SourceList<RumbleCommand> _rumbleCommands = new();
    private bool _outputEnabled;

    private int _pin;

    public Led(ConfigViewModel model, bool outputEnabled, int pin, Color ledOn,
        Color ledOff, byte[] ledIndices, RumbleCommand command) : base(model, new FixedInput(model, 0), ledOn, ledOff,
        ledIndices)
    {
        Pin = pin;
        OutputEnabled = outputEnabled;
        Command = command;
        _rumbleCommands.AddRange(Enum.GetValues<RumbleCommand>());
        _rumbleCommands.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.EmulationType, x => x.Model.RhythmType, x => x.Model.IsApa102).Select(FilterLeds))
            .Bind(out var rumbleCommands)
            .Subscribe();
        RumbleCommands = rumbleCommands;
        _ledRequiresColours = this.WhenAnyValue(x => x.Command).Select(s => s is not RumbleCommand.Ps4LightBar)
            .ToProperty(this, x => x.LedsRequireColours);
    }

    private readonly ObservableAsPropertyHelper<bool> _ledRequiresColours;

    public override bool LedsRequireColours => _ledRequiresColours.Value;

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

    private RumbleCommand _command;

    public RumbleCommand Command
    {
        get => _command;
        set
        {
            this.RaiseAndSetIfChanged(ref _command, value);
            UpdateDetails();
        }
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public ReadOnlyObservableCollection<RumbleCommand> RumbleCommands { get; }


    public override string LedOnLabel
    {
        get
        {
            if (Command is RumbleCommand.StageKitFog) return "Fog Inactive LED Colour";

            if (Command.IsPlayer() || Command.IsAuth()) return "LED Colour";

            if (Command is RumbleCommand.SantrollerStarPowerActive or RumbleCommand.SantrollerStarPowerFill)
                return "Start Power Gauge Full Colour";

            return "Active LED Colour";
        }
    }

    public override string LedOffLabel =>
        Command switch
        {
            RumbleCommand.StageKitFog => "Fog Inactive LED Colour",
            RumbleCommand.SantrollerStarPowerActive or RumbleCommand.SantrollerStarPowerFill =>
                "Start Power Gauge Empty Colour",
            _ => "Inactive LED Colour"
        };

    public override bool SupportsLedOff => !Command.IsAuth() && !Command.IsPlayer();

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

    public static Func<RumbleCommand, bool> FilterLeds((DeviceControllerType controllerType, EmulationType emulationType, RhythmType rhythmType, bool isApa102) type)
    {
        switch (type.emulationType)
        {
            case EmulationType.KeyboardMouse or EmulationType.BluetoothKeyboardMouse:
                return command => command.IsKeyboard();
            case EmulationType.Controller or EmulationType.Bluetooth:
                switch (type.controllerType)
                {
                    case DeviceControllerType.StageKit:
                        return command => command.IsStageKit() || command.IsPlayer() || command.IsAuth() || command.IsSantroller();
                    case DeviceControllerType.Gamepad:
                    case DeviceControllerType.ArcadeStick:
                    case DeviceControllerType.FlightStick:
                    case DeviceControllerType.DancePad:
                    case DeviceControllerType.ArcadePad:
                    case DeviceControllerType.Guitar:
                    case DeviceControllerType.LiveGuitar:
                    case DeviceControllerType.Drum when type.rhythmType is RhythmType.GuitarHero:
                        return command => !command.IsDrum() &&
                            (command.IsAuth() || command.IsPlayer() || command.IsSantroller() ||
                            // Lightbar only works with APA102s, as it requires full RGB
                            (type.isApa102 && command is RumbleCommand.Ps4LightBar));
                    case DeviceControllerType.Drum when type.rhythmType is RhythmType.RockBand:
                        return command => command != RumbleCommand.SantrollerOrange &&
                            (command.IsAuth() || command.IsPlayer() || command.IsSantroller() ||
                            // Lightbar only works with APA102s, as it requires full RGB
                            (type.isApa102 && command is RumbleCommand.Ps4LightBar));
                    case DeviceControllerType.Turntable:
                        return command => command.IsDj() || command.IsAuth() || command.IsPlayer() || command.IsSantroller();
                }

                break;
        }

        return _ => false;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedLed(LedOn, LedOff, LedIndices.ToArray(), Command, OutputEnabled, Pin);
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
                or ConfigField.KeyboardLed or ConfigField.LightBarLed)) return "";
        switch (mode)
        {
            case ConfigField.StrobeLed when Command is not RumbleCommand.StageKitStrobe:
            case ConfigField.PlayerLed when !Command.IsPlayer():
            case ConfigField.AuthLed when !Command.IsAuth():
            case ConfigField.RumbleLed when Command.IsPlayer() || Command.IsAuth():
            case ConfigField.LightBarLed when Command is not RumbleCommand.Ps4LightBar:
                return "";
            case ConfigField.KeyboardLed when !Command.IsKeyboard():
                return "";
            case ConfigField.LightBarLed:
                return string.Join("\n",
                    LedIndices.Select(index => Model.LedType.GetLedAssignment("red", "green", "blue", index)));
;        }

        var allOff = "(rumble_left == 0x00 rumble_right == 0xFF)";
        var on = "";
        var off = "";
        if (PinConfig != null)
        {
            on = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, true)+";";
            off = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, false)+";";
        }

        var between = "";
        foreach (var index in LedIndices)
        {
            on += $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, index)}";
            off += $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, index)}";
            between +=
                $@"ledState[{index - 1}].select = 1;{Model.LedType.GetLedAssignment(LedOn, LedOff, "rumble_left", index)}";
        }

        // Player and Auth commands are a set and forget type thing, they are never switched off after being turned on
        if (Command.IsPlayer())
            return $@"
                if (player == {(int) Command}) {{
                    {on}
                }}";

        if (Command.IsAuth()) return on;

        // On PS3 or 360, DJ leds respond to both values being the same. For PC reasons, we have a seperate euphoria command
        if (Command.IsDj())
            return $@"if (consoleType != UNIVERSAL && consoleType != XBOX360)) {{
                {between}
            }} else if ((rumble_right == {(int) RumbleCommand.DjEuphoria}) {{
                {between}
            }}";

        if (Command.IsSantroller())
            // Only support santroller commands on PC (either universal or xinput + windows)
            return
                $@"if ((consoleType == UNIVERSAL || consoleType == WINDOWS) && rumble_right == {(int) Command}) {{
                    if (rumble_left == 1) {{
                        {on}
                    }} else {{
                        {off}
                    }}
                }}";

        if (Command.IsKeyboard())
            return
                $@"if (leds & {1 << (Command - RumbleCommand.KeyboardNumLock)}) {{
                    {on}
                }} else {{
                    {off}
                }}";


        if (!Command.IsStageKit()) return "";
        switch (Command)
        {
            // Strobe is special because it well... strobes
            case RumbleCommand.StageKitStrobe:
                // In strobe mode, we just blink the led at a rate dictate by strobe_delay, only leaving it on for 10ms
                if (mode == ConfigField.StrobeLed)
                    return @$"if (last_strobe && last_strobe - millis() > stage_kit_millis[strobe_delay]) {{
                                last_strobe = millis();
                                {on}
                            }} else if (last_strobe && last_strobe - millis() > 10) {{
                                {off}
                            }}";

                // In rumble mode, we update strobe_delay based on packets receive, turning strobe off if requested
                return $@"
                        if (rumble_left == 0 && rumble_right >= 3 && rumble_right <= 7) {{
                            strobe_delay = 5 - (rumble_right - 2);
                        }}
                        if (strobe_delay == 0 || {allOff}) {{
                            strobe_delay = 0;
                            {off}
                        }}
                        ";

            // For fog, there is both an on and off command so we can just handle that 
            case RumbleCommand.StageKitFog:
                return $@"if (rumble_left == 0 && rumble_right == {StageKitFogCommands.Off} || {allOff}) {{
                              {off}
                          }} else if (rumble_left == 0 && rumble_right == {StageKitFogCommands.On}) {{
                              {on}
                          }}";
            // for the leds, we need to turn the led on if the bit is set, and off if it is not.
            case >= RumbleCommand.StageKitGreen1:
            {
                var led = 1 << ((int) Command & 0xf);
                var group = (int) Command & 0xf0;
                return @$"if ((rumble_left & {led} == 0) && (rumble_right == {group} || {allOff})) {{
                              {off}
                          }} else if (rumble_left & ({led}) && rumble_right == {group}) {{
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