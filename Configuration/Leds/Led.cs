using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Leds;

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
    [Description("1x Combo LED")] SantrollerCombo1,
    [Description("2x Combo LED")] SantrollerCombo2,
    [Description("3x Combo LED")] SantrollerCombo3,
    [Description("4x Combo LED")] SantrollerCombo4,
    [Description("6x Combo LED")] SantrollerCombo6,
    [Description("8x Combo LED")] SantrollerCombo8,
    [Description("Solo LED")] SantrollerSolo,
    [Description("Open Note LED")] SantrollerOpen,
    [Description("Green LED")] SantrollerGreen,
    [Description("Red LED")] SantrollerRed,
    [Description("Yellow LED")] SantrollerYellow,
    [Description("Blue LED")] SantrollerBlue,
    [Description("Orange LED")] SantrollerOrange,

    [Description("Star Power Gauge (Inactive) LED")]
    SantrollerStarPowerFill,

    [Description("Star Power Gauge (Active) LED")]
    SantrollerStarPowerActive,
    [Description("Num Lock LED")] KeyboardNumLock,
    [Description("Caps Lock LED")] KeyboardCapsLock,
    [Description("Scroll Lock LED")] KeyboardScrollLock,
    [Description("DJ Hero Euphoria LED")] DjEuphoria,
    [Description("Stage Kit Fog")] StageKitFog,

    [Description("Stage Kit Strobe Light")]
    StageKitStrobe,

    [Description("Stage Kit Strobe Green Led 1")]
    StageKitGreen1 = 0x40,

    [Description("Stage Kit Strobe Green Led 2")]
    StageKitGreen2,

    [Description("Stage Kit Strobe Green Led 3")]
    StageKitGreen3,

    [Description("Stage Kit Strobe Green Led 4")]
    StageKitGreen4,

    [Description("Stage Kit Strobe Green Led 5")]
    StageKitGreen5,

    [Description("Stage Kit Strobe Green Led 6")]
    StageKitGreen6,

    [Description("Stage Kit Strobe Green Led 7")]
    StageKitGreen7,

    [Description("Stage Kit Strobe Green Led 8")]
    StageKitGreen8,

    [Description("Stage Kit Strobe Red Led 1")]
    StageKitRed1 = 0x80,

    [Description("Stage Kit Strobe Red Led 2")]
    StageKitRed2,

    [Description("Stage Kit Strobe Red Led 3")]
    StageKitRed3,

    [Description("Stage Kit Strobe Red Led 4")]
    StageKitRed4,

    [Description("Stage Kit Strobe Red Led 5")]
    StageKitRed5,

    [Description("Stage Kit Strobe Red Led 6")]
    StageKitRed6,

    [Description("Stage Kit Strobe Red Led 7")]
    StageKitRed7,

    [Description("Stage Kit Strobe Red Led 8")]
    StageKitRed8,

    [Description("Stage Kit Strobe Yellow Led 1")]
    StageKitYellow1 = 0x60,

    [Description("Stage Kit Strobe Yellow Led 2")]
    StageKitYellow2,

    [Description("Stage Kit Strobe Yellow Led 3")]
    StageKitYellow3,

    [Description("Stage Kit Strobe Yellow Led 4")]
    StageKitYellow4,

    [Description("Stage Kit Strobe Yellow Led 5")]
    StageKitYellow5,

    [Description("Stage Kit Strobe Yellow Led 6")]
    StageKitYellow6,

    [Description("Stage Kit Strobe Yellow Led 7")]
    StageKitYellow7,

    [Description("Stage Kit Strobe Yellow Led 8")]
    StageKitYellow8,

    [Description("Stage Kit Strobe Blue Led 1")]
    StageKitBlue1 = 0x20,

    [Description("Stage Kit Strobe Blue Led 2")]
    StageKitBlue2,

    [Description("Stage Kit Strobe Blue Led 3")]
    StageKitBlue3,

    [Description("Stage Kit Strobe Blue Led 4")]
    StageKitBlue4,

    [Description("Stage Kit Strobe Blue Led 5")]
    StageKitBlue5,

    [Description("Stage Kit Strobe Blue Led 6")]
    StageKitBlue6,

    [Description("Stage Kit Strobe Blue Led 7")]
    StageKitBlue7,

    [Description("Stage Kit Strobe Blue Led 8")]
    StageKitBlue8
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
        Color ledOff, byte[] ledIndices, RumbleCommand command) : base(model, new FixedInput(model, 0), ledOn, ledOff, ledIndices)
    {
        Pin = pin;
        OutputEnabled = outputEnabled;
        Command = command;
        _rumbleCommands.AddRange(Enum.GetValues<RumbleCommand>());
        _rumbleCommands.Connect()
            .Filter(this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.EmulationType).Select(FilterLeds))
            .Bind(out var rumbleCommands)
            .Subscribe();
        RumbleCommands = rumbleCommands;
    }

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
        get=>_command;
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
    public override bool IsController => false;

    public override bool Valid => true;

    public override void Dispose()
    {
        if (PinConfig == null) return;
        Model.Microcontroller.UnAssignPins(PinConfig.Type);
        PinConfig = null;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Command);
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

    public static Func<RumbleCommand, bool> FilterLeds((DeviceControllerType, EmulationType) type)
    {
        switch (type.Item2)
        {
            case EmulationType.KeyboardMouse or EmulationType.BluetoothKeyboardMouse:
                return command => command.IsKeyboard();
            case EmulationType.StageKit:
                return command => command.IsStageKit() || command.IsPlayer();
            case EmulationType.Controller or EmulationType.Bluetooth:
                switch (type.Item1)
                {
                    case DeviceControllerType.Gamepad:
                    case DeviceControllerType.Wheel:
                    case DeviceControllerType.ArcadeStick:
                    case DeviceControllerType.FlightStick:
                    case DeviceControllerType.DancePad:
                    case DeviceControllerType.ArcadePad:
                    case DeviceControllerType.Guitar:
                    case DeviceControllerType.LiveGuitar:
                    case DeviceControllerType.Drum:
                        return command => command.IsAuth() || command.IsPlayer() || command.IsSantroller();
                    case DeviceControllerType.Turntable:
                        return command => command.IsDj() || command.IsAuth() || command.IsPlayer();
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

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is ConfigField.StrobeLed && Command != RumbleCommand.StageKitStrobe) return "";
        if (mode is not (ConfigField.StrobeLed or ConfigField.AuthLed or ConfigField.PlayerLed or ConfigField.RumbleLed
                or ConfigField.KeyboardLed) ||
            !AreLedsEnabled) return "";
        switch (mode)
        {
            case ConfigField.PlayerLed when !Command.IsPlayer():
            case ConfigField.AuthLed when !Command.IsAuth():
            case ConfigField.RumbleLed when Command.IsPlayer() || Command.IsAuth():
            case ConfigField.KeyboardLed when Command.IsKeyboard():
                return "";
        }

        var allOff = "(rumble_left == 0x00 rumble_right == 0xFF)";
        var on = "";
        var off = "";
        if (PinConfig != null)
        {
            on = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, true);
            off = Model.Microcontroller.GenerateDigitalWrite(PinConfig.Pin, false);
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
            return $@"if (consoleType != UNIVERSAL && (controllerType != WINDOWS_XBOX360 || !windows_or_xbox_one))) {{
                {between}
            }} else if ((rumble_right == {(int) RumbleCommand.DjEuphoria}) {{
                {between}
            }}";

        if (Command.IsSantroller())
            // Only support santroller commands on PC (either universal or xinput + windows)
            return
                $@"if ((consoleType == UNIVERSAL || (controllerType == WINDOWS_XBOX360 && windows_or_xbox_one)) && rumble_right == {(int) Command}) {{
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