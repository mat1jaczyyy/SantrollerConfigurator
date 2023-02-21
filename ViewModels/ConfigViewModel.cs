using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Leds;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using ProtoBuf;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class ConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public static readonly string Apa102SpiType = "APA102";

    private readonly ObservableAsPropertyHelper<bool> _isApa102;
    private readonly ObservableAsPropertyHelper<bool> _isController;
    private readonly ObservableAsPropertyHelper<bool> _isKeyboard;
    private readonly ObservableAsPropertyHelper<bool> _isRf;
    private readonly ObservableAsPropertyHelper<bool> _isRhythm;

    private readonly ObservableAsPropertyHelper<string?> _writeToolTip;

    private SpiConfig? _apa102SpiConfig;

    private bool _combinedDebounce;

    private DeviceControllerType _deviceControllerType;

    private EmulationType _emulationType;

    private bool _fininalised;

    private bool _hasError;

    private byte _ledCount;

    private LedType _ledType;

    private MouseMovementType _mouseMovementType;

    private DirectPinConfig? _rfCe;

    private byte _rfChannel;

    private byte _rfId;

    private DirectPinConfig? _rfCsn;

    private SpiConfig? _rfSpiConfig;

    private RhythmType _rhythmType;

    private bool _xinputOnWindows;

    public ConfigViewModel(MainWindowViewModel screen)
    {
        Microcontroller = screen.SelectedDevice!.GetMicrocontroller(this);
        ShowIssueDialog = new Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?>();
        ShowUnoShortDialog = new Interaction<Arduino, ShowUnoShortWindowViewModel?>();
        ShowYesNoDialog =
            new Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>();
        ShowBindAllDialog =
            new Interaction<(ConfigViewModel model, Output output, DirectInput
                input), BindAllWindowViewModel>();
        BindAllCommand = ReactiveCommand.CreateFromTask(BindAll);
        Main = screen;
        HostScreen = screen;

        WriteConfig = ReactiveCommand.CreateFromObservable(Write,
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true, Item3: false}));
        GoBack = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel?>(Main.GoBack.Execute);
        Bindings = new AvaloniaList<Output>();

        _writeToolTip = this.WhenAnyValue(x => x.HasError)
            .Select(s => s ? "There are errors in your configuration" : null).ToProperty(this, s => s.WriteToolTip);

        _isRf = this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.RfController or EmulationType.RfKeyboardMouse)
            .ToProperty(this, x => x.IsRf);
        _isRhythm = this.WhenAnyValue(x => x.DeviceType)
            .Select(x => x is DeviceControllerType.Drum or DeviceControllerType.Guitar)
            .ToProperty(this, x => x.IsRhythm);
        _isController = this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.Controller or EmulationType.Bluetooth)
            .ToProperty(this, x => x.IsController);
        _isKeyboard = this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.KeyboardMouse or EmulationType.BluetoothKeyboardMouse)
            .ToProperty(this, x => x.IsKeyboard);
        _isApa102 = this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.APA102_BGR or LedType.APA102_BRG or LedType.APA102_GBR or LedType.APA102_GRB
                or LedType.APA102_RBG or LedType.APA102_RGB)
            .ToProperty(this, x => x.IsApa102);

        if (!screen.SelectedDevice!.LoadConfiguration(this)) SetDefaults();
    }

    public Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?> ShowIssueDialog { get; }

    public Interaction<Arduino, ShowUnoShortWindowViewModel?> ShowUnoShortDialog { get; }

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel> ShowYesNoDialog { get; }

    public Interaction<(ConfigViewModel model, Output output, DirectInput input),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; }

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    public IEnumerable<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>();

    public IEnumerable<RhythmType> RhythmTypes => Enum.GetValues<RhythmType>();

    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            Main.SelectedDevice!.IsPico() ||
            (type != EmulationType.Bluetooth && type != EmulationType.BluetoothKeyboardMouse));

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();

    //TODO: actually read and write this as part of the config
    public bool KvEnabled { get; set; } = false;
    public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
    public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

    public ICommand WriteConfig { get; }

    public ICommand GoBack { get; }

    public MouseMovementType MouseMovementType
    {
        get => _mouseMovementType;
        set => this.RaiseAndSetIfChanged(ref _mouseMovementType, value);
    }

    public int Apa102Mosi
    {
        get => _apa102SpiConfig?.Mosi ?? 0;
        set => _apa102SpiConfig!.Mosi = value;
    }

    public int Apa102Sck
    {
        get => _apa102SpiConfig?.Sck ?? 0;
        set => _apa102SpiConfig!.Sck = value;
    }

    public int RfMosi
    {
        get => _rfSpiConfig?.Mosi ?? 0;
        set => _rfSpiConfig!.Mosi = value;
    }

    public int RfMiso
    {
        get => _rfSpiConfig?.Miso ?? 0;
        set => _rfSpiConfig!.Miso = value;
    }

    public int RfSck
    {
        get => _rfSpiConfig?.Sck ?? 0;
        set => _rfSpiConfig!.Sck = value;
    }

    public int RfCe
    {
        get => _rfCe?.Pin ?? 0;
        set => _rfCe!.Pin = value;
    }

    public int RfCsn
    {
        get => _rfCsn?.Pin ?? 0;
        set => _rfCsn!.Pin = value;
    }

    public byte LedCount
    {
        get => _ledCount;
        set => this.RaiseAndSetIfChanged(ref _ledCount, value);
    }


    public byte RfId
    {
        get => _rfId;
        set => this.RaiseAndSetIfChanged(ref _rfId, value);
    }

    public byte RfChannel
    {
        get => _rfChannel;
        set => this.RaiseAndSetIfChanged(ref _rfChannel, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }

    public bool Finalised
    {
        get => _fininalised;
        set => this.RaiseAndSetIfChanged(ref _fininalised, value);
    }

    public LedType LedType
    {
        get => _ledType;
        set
        {
            if (value == LedType.None)
            {
                Microcontroller.UnAssignPins(Apa102SpiType);
            }
            else if (_ledType == LedType.None)
            {
                var pins = Microcontroller.SpiPins(Apa102SpiType);
                var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
                _apa102SpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, mosi, -1, sck, true, true,
                    true,
                    Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000))!;
                this.RaisePropertyChanged(nameof(Apa102Mosi));
                this.RaisePropertyChanged(nameof(Apa102Sck));
            }

            this.RaiseAndSetIfChanged(ref _ledType, value);
        }
    }

    public bool XInputOnWindows
    {
        get => _xinputOnWindows;
        set => this.RaiseAndSetIfChanged(ref _xinputOnWindows, value);
    }

    public bool CombinedDebounce
    {
        get => _combinedDebounce;
        set => this.RaiseAndSetIfChanged(ref _combinedDebounce, value);
    }

    public DeviceControllerType DeviceType
    {
        get => _deviceControllerType;
        set
        {
            this.RaiseAndSetIfChanged(ref _deviceControllerType, value);
            UpdateBindings();
        }
    }

    public EmulationType EmulationType
    {
        get => _emulationType;
        set => SetDefaultBindings(value);
    }

    public RhythmType RhythmType
    {
        get => _rhythmType;
        set
        {
            this.RaiseAndSetIfChanged(ref _rhythmType, value);
            UpdateBindings();
        }
    }

    public Microcontroller Microcontroller { get; }

    public AvaloniaList<Output> Bindings { get; }
    public bool IsRhythm => _isRhythm.Value;
    public bool IsController => _isController.Value;
    public bool IsKeyboard => _isKeyboard.Value;
    public bool IsApa102 => _isApa102.Value;
    public bool BindableSpi => Microcontroller.SpiAssignable;
    public bool IsRf => _isRf.Value;
    public string? WriteToolTip => _writeToolTip.Value;

    public List<int> AvailableApaMosiPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaSckPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfMosiPins => Microcontroller.SpiPins(RFRXOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Miso)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfMisoPins => Microcontroller.SpiPins(RFRXOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfSckPins => Microcontroller.SpiPins(RFRXOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailablePins => Microcontroller.GetAllPins(false);
    public IEnumerable<PinConfig> PinConfigs => new[] {_apa102SpiConfig!};
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

    public IScreen HostScreen { get; }


    public void AddLedBinding()
    {
        var first = Enum.GetValues<RumbleCommand>().Where(Led.FilterLeds((DeviceType, EmulationType))).First();
        Bindings.Add(new Led(this, false, 0, Colors.Black, Colors.Black, Array.Empty<byte>(),
            first));
    }

    public void SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceControllerType type, RhythmType rhythmType,
        EmulationType emulationType)
    {
        this.RaiseAndSetIfChanged(ref _deviceControllerType, type, nameof(DeviceType));
        this.RaiseAndSetIfChanged(ref _rhythmType, rhythmType, nameof(RhythmType));
        this.RaiseAndSetIfChanged(ref _emulationType, emulationType, nameof(EmulationType));
    }

    private void UpdateBindings()
    {
        foreach (var binding in Bindings) binding.UpdateBindings();

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, _rhythmType, Bindings);
        Bindings.RemoveAll(extra);
        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Any(s => s is WiiCombinedOutput or Ps2CombinedOutput or RFRXOutput)) return;


        if (_deviceControllerType == DeviceControllerType.Drum)
        {
            IEnumerable<DrumAxisType> difference = DrumAxisTypeMethods.GetDifferenceFor(_rhythmType).ToHashSet();
            Bindings.RemoveAll(Bindings.Where(s => s is DrumAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveAll(Bindings.Where(s => s is DrumAxis));
        }

        if (_deviceControllerType is DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar)
        {
            IEnumerable<GuitarAxisType> difference = GuitarAxisTypeMethods
                .GetDifferenceFor(_rhythmType, _deviceControllerType).ToHashSet();
            Bindings.RemoveAll(Bindings.Where(s => s is GuitarAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveAll(Bindings.Where(s => s is GuitarAxis));
        }

        if (_deviceControllerType is not DeviceControllerType.Guitar || _rhythmType is not RhythmType.RockBand)
            Bindings.RemoveAll(Bindings.Where(s => s is RbButton));

        if (_deviceControllerType == DeviceControllerType.Turntable)
            if (!Bindings.Any(s => s is DjCombinedOutput))
                Bindings.Add(new DjCombinedOutput(this));

        foreach (var type in types)
            switch (type)
            {
                case StandardButtonType buttonType:
                    Bindings.Add(new ControllerButton(this,
                        new DirectInput(0, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType));
                    break;
                case RBButtonType buttonType:
                    Bindings.Add(new RbButton(this,
                        new DirectInput(0, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType));
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, new DirectInput(Microcontroller.GetFirstAnalogPin(),
                            DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, 64, 10, axisType));
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
            }
    }


    public IObservable<PlatformIo.PlatformIoState> Write()
    {
        return Main.Write(this);
    }

    public void SetDefaults()
    {
        ClearOutputs();
        LedType = LedType.None;
        _deviceControllerType = DeviceControllerType.Gamepad;
        _emulationType = EmulationType.Controller;
        _rhythmType = RhythmType.GuitarHero;
        this.RaisePropertyChanged(nameof(DeviceType));
        this.RaisePropertyChanged(nameof(EmulationType));
        this.RaisePropertyChanged(nameof(RhythmType));
        XInputOnWindows = true;
        MouseMovementType = MouseMovementType.Relative;

        switch (Main.DeviceInputType)
        {
            case DeviceInputType.Direct:
                SetDefaultBindings(EmulationType);
                break;
            case DeviceInputType.Wii:
                Bindings.Add(new WiiCombinedOutput(this));
                break;
            case DeviceInputType.Ps2:
                Bindings.Add(new Ps2CombinedOutput(this));
                break;
            case DeviceInputType.Rf:
                Bindings.Add(new RFRXOutput(this));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (Main.IsUno || Main.IsMega)
        {
            Write();
            _ = ShowUnoShortDialog.Handle((Arduino) Main.SelectedDevice!).ToTask();
            return;
        }

        UpdateErrors();

        Write();
    }

    private async void SetDefaultBindings(EmulationType emulationType)
    {
        if (IsRf)
        {
            if (_rfSpiConfig == null)
            {
                var pins = Microcontroller.SpiPins(RFRXOutput.SpiType);
                var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                var miso = pins.First(pair => pair.Value is SpiPinType.Miso).Key;
                var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
                _rfSpiConfig = Microcontroller.AssignSpiPins(this, RFRXOutput.SpiType, mosi, miso, sck, true, true,
                    true,
                    4000000);
                this.RaisePropertyChanged(nameof(RfMiso));
                this.RaisePropertyChanged(nameof(RfMosi));
                this.RaisePropertyChanged(nameof(RfSck));
                var first = Microcontroller.GetAllPins(false).First();
                _rfCe = Microcontroller.GetOrSetPin(this, RFRXOutput.SpiType + "_ce", first, DevicePinMode.PullUp);
                _rfCsn = Microcontroller.GetOrSetPin(this, RFRXOutput.SpiType + "_csn", first, DevicePinMode.Output);
            }
        }
        else
        {
            if (_rfSpiConfig != null)
            {
                Microcontroller.UnAssignPins(_rfSpiConfig.Type);
                _rfSpiConfig = null;
            }

            if (_rfCe != null)
            {
                Microcontroller.UnAssignPins(_rfCe.Type);
                _rfCe = null;
            }

            if (_rfCsn != null)
            {
                Microcontroller.UnAssignPins(_rfCsn.Type);
                _rfCsn = null;
            }
        }

        if (Bindings.Any())
        {
            var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
                "The following action will clear all your bindings, are you sure you want to do this?")).ToTask();
            if (!yesNo.Response)
            {
                this.RaisePropertyChanged(nameof(EmulationType));
                return;
            }

            ;
        }

        _emulationType = emulationType;
        this.RaisePropertyChanged(nameof(EmulationType));
        ClearOutputs();
        if (emulationType == EmulationType.StageKit)
        {
            DeviceType = DeviceControllerType.Gamepad;
            return;
        }

        if (EmulationType is EmulationType.KeyboardMouse or EmulationType.BluetoothKeyboardMouse) return;
        foreach (var type in Enum.GetValues<StandardAxisType>())
        {
            if (ControllerEnumConverter.GetAxisText(_deviceControllerType, _rhythmType, type) == null) continue;
            if (DeviceType == DeviceControllerType.Turntable &&
                type is StandardAxisType.LeftStickX or StandardAxisType.LeftStickY) continue;
            Bindings.Add(new ControllerAxis(this,
                new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                type));
        }

        foreach (var type in Enum.GetValues<StandardButtonType>())
        {
            if (ControllerEnumConverter.GetButtonText(_deviceControllerType, _rhythmType, type) ==
                null) continue;
            Bindings.Add(new ControllerButton(this,
                new DirectInput(0, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, type));
        }

        UpdateErrors();
    }

    public void Finalise()
    {
        Finalised = true;
    }

    public void Generate(PlatformIo pio)
    {
        var outputs = Bindings.SelectMany(binding => binding.Outputs.Items).ToList();
        var inputs = outputs.Select(binding => binding.Input?.InnermostInput()).OfType<Input>().ToList();
        var directInputs = inputs.OfType<DirectInput>().ToList();
        var configFile = Path.Combine(pio.FirmwareDir, "include", "config_data.h");
        var lines = new List<string>();
        var leds = outputs.SelectMany(s => s.Outputs.Items).SelectMany(s => s.LedIndices).ToList();

        using (var outputStream = new MemoryStream())
        {
            using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(this));
            }

            lines.Add(
                $"#define CONFIGURATION {{{string.Join(",", outputStream.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
            lines.Add($"#define CONFIGURATION_LEN {outputStream.ToArray().Length}");
        }


        lines.Add($"#define WINDOWS_USES_XINPUT {XInputOnWindows.ToString().ToLower()}");

        lines.Add(
            $"#define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}");

        lines.Add($"#define TICK_SHARED {GenerateTick(ConfigField.Shared)}");

        lines.Add($"#define TICK_PS3 {GenerateTick(ConfigField.Ps3)}");

        lines.Add($"#define TICK_XINPUT {GenerateTick(ConfigField.Xbox360)}");

        lines.Add($"#define TICK_XBOX_ONE {GenerateTick(ConfigField.XboxOne)}");

        lines.Add($"#define PS3_MASK {GenerateTick(ConfigField.Ps3Mask)}");

        lines.Add($"#define XINPUT_MASK {GenerateTick(ConfigField.Xbox360Mask)}");

        lines.Add($"#define XBOX_ONE_MASK {GenerateTick(ConfigField.XboxOneMask)}");

        lines.Add($"#define CONSUMER_MASK {GenerateTick(ConfigField.ConsumerMask)}");

        lines.Add($"#define MOUSE_MASK {GenerateTick(ConfigField.MouseMask)}");

        lines.Add($"#define KEYBOARD_MASK {GenerateTick(ConfigField.KeyboardMask)}");

        var nkroTick = GenerateTick(ConfigField.Keyboard);
        if (nkroTick.Any()) lines.Add($"#define TICK_NKRO {nkroTick}");

        var consumerTick = GenerateTick(ConfigField.Consumer);
        if (consumerTick.Any()) lines.Add($"#define TICK_CONSUMER {consumerTick}");

        var mouseTick = GenerateTick(ConfigField.Mouse);
        if (mouseTick.Any()) lines.Add($"#define TICK_MOUSE {mouseTick}");

        lines.Add(
            $"#define ADC_COUNT {directInputs.DistinctBy(s => s.PinConfig.Pin).Count(input => input.IsAnalog)}");

        lines.Add($"#define DIGITAL_COUNT {CalculateDebounceTicks()}");
        lines.Add($"#define LED_COUNT {LedCount}");

        lines.Add($"#define LED_TYPE {GetLedType()}");

        lines.Add(GenerateTick(ConfigField.RfRx));
        if (IsApa102)
        {
            lines.Add($"#define {Apa102SpiType.ToUpper()}_SPI_PORT {_apa102SpiConfig!.Definition}");

            lines.Add($"#define TICK_LED {GenerateLedTick()}");
        }

        if (IsRf)
        {
            lines.Add($"#define TRANSMIT_RADIO_ID {RfId}");
            lines.Add($"#define DEST_RADIO_ID {RfChannel}");
            lines.Add("#define RF_TX");
            lines.Add($"#define RADIO_CE {_rfCe!.Pin}");
            lines.Add($"#define RADIO_CSN {_rfCsn!.Pin}");
            if (BindableSpi)
            {
                lines.Add($"#define RADIO_MOSI {_rfSpiConfig!.Mosi}");
                lines.Add($"#define RADIO_MISO {_rfSpiConfig!.Miso}");
                lines.Add($"#define RADIO_SCK {_rfSpiConfig!.Sck}");
            }
        }

        lines.Add($"#define HANDLE_AUTH_LED {GenerateTick(ConfigField.AuthLed)}");

        lines.Add($"#define HANDLE_PLAYER_LED {GenerateTick(ConfigField.PlayerLed)}");

        lines.Add($"#define HANDLE_RUMBLE {GenerateTick(ConfigField.RumbleLed)}");

        lines.Add($"#define HANDLE_KEYBOARD_LED {GenerateTick(ConfigField.KeyboardLed)}");

        lines.Add($"#define CONSOLE_TYPE {GetEmulationType()}");

        lines.Add($"#define DEVICE_TYPE {(byte) DeviceType}");

        lines.Add($"#define RHYTHM_TYPE {(byte) RhythmType}");
        lines.Add(
            $"#define BLUETOOTH {(EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse).ToString().ToLower()}");
        if (KvEnabled)
        {
            lines.Add(
                $"#define KV_KEY_1 {{{string.Join(",", KvKey1.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
            lines.Add(
                $"#define KV_KEY_2 {{{string.Join(",", KvKey2.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
        }

        lines.Add(Ps2Input.GeneratePs2Pressures(inputs));

        // Sort by pin index, and then map to adc number and turn into an array
        lines.Add(
            $"#define ADC_PINS {{{string.Join(",", directInputs.Where(s => s.IsAnalog).OrderBy(s => s.PinConfig.Pin).Select(s => Microcontroller.GetChannel(s.PinConfig.Pin, false).ToString()).Distinct())}}}");

        lines.Add($"#define PIN_INIT {Microcontroller.GenerateInit()}");

        lines.Add(Microcontroller.GenerateDefinitions());

        lines.Add($"#define ARDWIINO_BOARD \"{Microcontroller.Board.ArdwiinoName}\"");
        lines.Add(string.Join("\n",
            inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}")));

        File.WriteAllLines(configFile, lines);
    }

    private byte GetEmulationType()
    {
        return (byte) GetSimpleEmulationType();
    }

    public EmulationType GetSimpleEmulationType()
    {
        switch (EmulationType)
        {
            case EmulationType.Bluetooth:
            case EmulationType.Controller:
            case EmulationType.RfController:
                return EmulationType.Controller;
            case EmulationType.KeyboardMouse:
            case EmulationType.BluetoothKeyboardMouse:
            case EmulationType.RfKeyboardMouse:
                return EmulationType.KeyboardMouse;
            default:
                return EmulationType;
        }
    }

    private int GetLedType()
    {
        switch (LedType)
        {
            case LedType.APA102_RGB:
            case LedType.APA102_RBG:
            case LedType.APA102_GRB:
            case LedType.APA102_GBR:
            case LedType.APA102_BRG:
            case LedType.APA102_BGR:
                return 1;
            case LedType.None:
            default:
                return 0;
        }
    }

    private async Task BindAll()
    {
        foreach (var binding in Bindings)
        {
            if (binding.Input?.InnermostInput() is not DirectInput direct) continue;
            var response = await ShowBindAllDialog.Handle((this, binding, direct)).ToTask();
            if (!response.Response) return;
        }
    }

    public void RemoveOutput(Output output)
    {
        output.Dispose();
        if (Bindings.Remove(output))
        {
            UpdateErrors();
            return;
        }

        foreach (var binding in Bindings) binding.Outputs.Remove(output);

        UpdateErrors();
    }

    public void ClearOutputs()
    {
        foreach (var binding in Bindings) binding.Dispose();

        Bindings.Clear();
        UpdateErrors();
    }

    public async void ClearOutputsWithConfirmation()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        foreach (var binding in Bindings) binding.Dispose();

        Bindings.Clear();
        UpdateErrors();
    }

    public void ExpandAll()
    {
        foreach (var binding in Bindings) binding.Expanded = true;
    }

    public void CollapseAll()
    {
        foreach (var binding in Bindings) binding.Expanded = false;
    }

    public async void ResetWithConfirmation()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        foreach (var binding in Bindings) binding.Dispose();

        Bindings.Clear();
        UpdateBindings();
        UpdateErrors();
    }

    public async void Reset()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Reset", "Cancel",
                "The following action will revert your device back to an Arduino, are you sure you want to do this?"))
            .ToTask();
        if (!yesNo.Response) return;
        //TODO: actually revert the device to an arduino
    }

    public void AddOutput()
    {
        if (IsController)
            Bindings.Add(new EmptyOutput(this));
        else if (IsKeyboard)
            Bindings.Add(new KeyboardButton(this, new DirectInput(0, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, Key.Space));

        UpdateErrors();
    }

    private string GenerateLedTick()
    {
        var outputs = Bindings.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (_ledType == LedType.None ||
            !outputs.Any(s => s.LedIndices.Any())) return "";
        var ledMax = outputs.SelectMany(output => output.LedIndices).Max();
        var ret =
            "spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);";
        for (var i = 0; i <= ledMax; i++)
            ret +=
                $"spi_transfer(APA102_SPI_PORT, 0xff);spi_transfer(APA102_SPI_PORT, ledState[{i}].r);spi_transfer(APA102_SPI_PORT, ledState[{i}].g);spi_transfer(APA102_SPI_PORT, ledState[{i}].b);";

        for (var i = 0; i <= ledMax; i += 16) ret += "spi_transfer(APA102_SPI_PORT, 0xff);";

        return ret.Replace('\n', ' ') + GenerateTick(ConfigField.StrobeLed);
    }

    private string GenerateTick(ConfigField mode)
    {
        var outputs = Bindings.SelectMany(binding => binding.ValidOutputs()).ToList();
        var groupedOutputs = outputs
            .SelectMany(s => s.Input?.Inputs().Zip(Enumerable.Repeat(s, s.Input?.Inputs().Count ?? 0))!)
            .GroupBy(s => s.First.InnermostInput().GetType()).ToList();
        var combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;

        Dictionary<string, int> debounces = new();
        if (combined)
            foreach (var output in outputs.Where(output => output.IsStrum))
                debounces[output.Name] = debounces.Count;

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new List<Output>();
        foreach (var groupedOutput in groupedOutputs)
        foreach (var (input, output) in groupedOutput)
        {
            var generatedInput = input.Generate(mode);
            if (input == null) throw new IncompleteConfigurationException("Missing input!");
            if (output is not OutputButton and not DrumAxis) continue;

            if (output.Input is MacroInput)
            {
                if (!debounces.ContainsKey(output.Name + generatedInput))
                    debounces[output.Name + generatedInput] = debounces.Count;

                macros.Add(output);
            }
            else
            {
                if (!debounces.ContainsKey(output.Name)) debounces[output.Name] = debounces.Count;
            }

            if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

            inputs[generatedInput].Add(debounces[output.Name]);
        }

        var seen = new HashSet<Output>();
        // Handle most mappings
        // Sort in a way that any digital to analog based groups are last. This is so that seenAnalog will be filled in when necessary.
        var ret = groupedOutputs.OrderByDescending(s => s.Count(s2 => s2.Second.Input is DigitalToAnalog))
            .Aggregate("", (current, group) =>
            {
                // we need to ensure that DigitalToAnalog is last
                return current + group
                    .First().First.InnermostInput()
                    .GenerateAll(Bindings.ToList(), group.OrderByDescending(s => s.First is DigitalToAnalog ? 0 : 1)
                        .Select(s =>
                        {
                            var input = s.First;
                            var output = s.Second;
                            var generatedInput = input.Generate(mode);
                            var index = new List<int> {0};
                            var extra = "";
                            if (output is OutputButton or DrumAxis)
                            {
                                index = new List<int> {debounces[output.Name]};
                                if (output.Input is MacroInput)
                                {
                                    if (mode == ConfigField.Shared)
                                    {
                                        output = output.Serialize().Generate(this);
                                        output.Input = input;
                                        index = new List<int> {debounces[output.Name + generatedInput]};
                                    }
                                    else
                                    {
                                        if (seen.Contains(output)) return new Tuple<Input, string>(input, "");
                                        seen.Add(output);
                                        index = output.Input!.Inputs()
                                            .Select(input1 => debounces[output.Name + input1.Generate(mode)])
                                            .ToList();
                                    }
                                }
                            }

                            var generated = output.Generate(mode, index, combined, extra);

                            if (output is OutputAxis axis && mode != ConfigField.Shared)
                                generated = generated.Replace("{output}", axis.GenerateOutput(mode));

                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .ToList(), mode);
            });
        // Flick off intersecting outputs when multiple buttons are pressed
        if (mode == ConfigField.Shared)
            foreach (var output in macros)
            {
                var ifStatement = string.Join(" && ",
                    output.Input!.Inputs().Select(input =>
                        $"debounce[{debounces[output.Name + input.Generate(mode)]}]"));
                var sharedReset = output.Input!.Inputs().Aggregate("",
                    (current, input) => current + string.Join("",
                        inputs[input.Generate(mode)].Select(s => $"debounce[{s}]=0;").Distinct()));
                ret += @$"if ({ifStatement}) {{{sharedReset}}}";
            }

        return ret.Replace('\n', ' ').Trim();
    }

    private int CalculateDebounceTicks()
    {
        var outputs = Bindings.SelectMany(binding => binding.ValidOutputs()).ToList();
        var groupedOutputs = outputs
            .SelectMany(s => s.Input?.Inputs().Zip(Enumerable.Repeat(s, s.Input?.Inputs().Count ?? 0))!)
            .GroupBy(s => s.First.InnermostInput().GetType()).ToList();
        var combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;

        Dictionary<string, int> debounces = new();
        if (combined)
            foreach (var output in outputs.Where(output => output.IsStrum))
                debounces[output.Name] = debounces.Count;

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new List<Output>();
        foreach (var groupedOutput in groupedOutputs)
        foreach (var (input, output) in groupedOutput)
        {
            var generatedInput = input.Generate(ConfigField.Xbox360);
            if (input == null) throw new IncompleteConfigurationException("Missing input!");
            if (output is not OutputButton and not DrumAxis) continue;

            if (output.Input is MacroInput)
            {
                if (!debounces.ContainsKey(output.Name + generatedInput))
                    debounces[output.Name + generatedInput] = debounces.Count;

                macros.Add(output);
            }
            else
            {
                if (!debounces.ContainsKey(output.Name)) debounces[output.Name] = debounces.Count;
            }

            if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

            inputs[generatedInput].Add(debounces[output.Name]);
        }

        return debounces.Count;
    }

    public bool IsCombinedChild(Output output)
    {
        return !Bindings.Contains(output);
    }

    public Dictionary<string, List<int>> GetPins(string type)
    {
        var pins = new Dictionary<string, List<int>>();
        foreach (var binding in Bindings)
        {
            var configs = binding.GetPinConfigs();
            //Exclude digital or analog pins (which use a guid containing a -
            if (configs.Any(s => s.Type == type || (type.Contains("-") && s.Type.Contains("-")))) continue;
            if (!pins.ContainsKey(binding.Name)) pins[binding.Name] = new List<int>();

            foreach (var pinConfig in configs) pins[binding.Name].AddRange(pinConfig.Pins);
        }

        if (IsApa102 && _apa102SpiConfig != null) pins["APA102"] = _apa102SpiConfig.Pins.ToList();

        return pins;
    }

    public void UpdateErrors()
    {
        var foundError = false;
        foreach (var output in Bindings)
        {
            output.UpdateErrors();
            if (!string.IsNullOrEmpty(output.ErrorText)) foundError = true;
        }

        HasError = foundError;
    }
}