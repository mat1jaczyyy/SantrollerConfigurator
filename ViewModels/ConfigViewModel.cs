using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class ConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public static readonly string Apa102SpiType = "APA102";
    public static readonly string UsbHostPinTypeDm = "DM";
    public static readonly string UsbHostPinTypeDp = "DP";
    public static readonly string UnoPinTypeTx = "Uno Serial Tx Pin";
    public static readonly string UnoPinTypeRx = "Uno Serial Rx Pin";
    public static readonly int UnoPinTypeRxPin = 0;
    public static readonly int UnoPinTypeTxPin = 1;

    private bool _allExpanded;


    private SpiConfig? _apa102SpiConfig;

    private EmulationType _emulationType;

    private LedType _ledType;

    private readonly DirectPinConfig? _unoRx;
    private readonly DirectPinConfig? _unoTx;

    private readonly DirectPinConfig _usbHostDm;
    private readonly DirectPinConfig _usbHostDp;

    public ConfigViewModel(MainWindowViewModel screen, IConfigurableDevice device, bool branded)
    {
        Device = device;
        Main = screen;
        Branded = branded;
        if (device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        else
            LocalAddress = "Write config to retrieve address";

        HostScreen = screen;
        Microcontroller = device.GetMicrocontroller(this);
        BindAllCommand = ReactiveCommand.CreateFromTask(BindAllAsync);

        WriteConfigCommand = ReactiveCommand.CreateFromObservable(() => Main.Write(this),
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true, Item3: false}));
        ResetCommand = ReactiveCommand.CreateFromTask(ResetAsync,
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true}));
        GoBackCommand = ReactiveCommand.Create(GoBack, this.WhenAnyValue(x => x.Main.Working).Select(s => !s));

        SaveConfigCommand = ReactiveCommand.CreateFromObservable(() => SaveConfig.Handle(this));

        LoadConfigCommand = ReactiveCommand.CreateFromObservable(() => LoadConfig.Handle(this));
        this.WhenAnyValue(x => x.Deque, x => x.PollRate)
            .Select(GeneratePollRateLabel)
            .ToPropertyEx(this, x => x.PollRateLabel);
        this.WhenAnyValue(x => x.HasError)
            .Select(s => s ? "There are errors in your configuration" : null).ToPropertyEx(this, s => s.WriteToolTip);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Advanced)
            .ToPropertyEx(this, x => x.IsAdvancedMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Standard)
            .ToPropertyEx(this, x => x.IsStandardMode);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
            .ToPropertyEx(this, x => x.IsBluetooth);
        this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.LiveGuitar or DeviceControllerType.GuitarHeroGuitar
                or DeviceControllerType.RockBandGuitar)
            .ToPropertyEx(this, x => x.IsGuitar);
        this.WhenAnyValue(x => x.DeviceControllerType)
            .Select(x => x is DeviceControllerType.StageKit)
            .ToPropertyEx(this, x => x.IsStageKit);
        _strumDebounceDisplay = this.WhenAnyValue(x => x.StrumDebounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.StrumDebounceDisplay);
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.Controller)
            .ToPropertyEx(this, x => x.IsController);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.KeyboardMouse)
            .ToPropertyEx(this, x => x.IsKeyboard);
        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb)
            .ToPropertyEx(this, x => x.IsApa102);
        Bindings.Connect()
            .Filter(s => s.IsVisible)
            .Bind(out var outputs)
            .Subscribe();
        Outputs = outputs;
        SupportsReset = !device.IsMini() && !device.IsEsp32();
        _usbHostDm = new DirectPinConfig(this, UsbHostPinTypeDm, -1, DevicePinMode.Skip);
        _usbHostDp = new DirectPinConfig(this, UsbHostPinTypeDp, -1, DevicePinMode.Skip);
        if (!device.LoadConfiguration(this))
        {
            SetDefaults();
        }

        if (Main is {IsUno: false, IsMega: false}) return;
        _unoRx = new DirectPinConfig(this, UnoPinTypeRx, UnoPinTypeRxPin, DevicePinMode.Output);
        _unoTx = new DirectPinConfig(this, UnoPinTypeTx, UnoPinTypeTxPin, DevicePinMode.Output);
    }

    private static string GeneratePollRateLabel((bool dequeue, int rate) arg)
    {
        var rate = Math.Floor((1f / Math.Max(arg.rate, 1)) * 1000);
        return arg.dequeue ? $"Dequeue Rate ({rate}+ fps required)" : $"Poll Rate (0 for fastest speed) ({rate}hz)";
    }

    public IConfigurableDevice Device { get; private set; }

    public ReadOnlyObservableCollection<Output> Outputs { get; }

    public bool SupportsReset { get; }
    
    public bool Branded { get; }


    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    private readonly ObservableAsPropertyHelper<float> _strumDebounceDisplay;
    private DeviceControllerType _deviceControllerType;

    public float DebounceDisplay
    {
        get => _debounceDisplay.Value;
        set => Debounce = (int) (value * 10);
    }

    public float StrumDebounceDisplay
    {
        get => _strumDebounceDisplay.Value;
        set => StrumDebounce = (int) (value * 10);
    }

    [Reactive] public string Variant { get; set; } = "";
    [Reactive] public bool SwapSwitchFaceButtons { get; set; }

    [Reactive] public bool CombinedStrumDebounce { get; set; }
    [Reactive] public string? RfErrorText { get; set; }
    [Reactive] public string? UsbHostErrorText { get; set; }
    [Reactive] public string? Apa102ErrorText { get; set; }

    public bool AllExpanded
    {
        get => _allExpanded;
        set
        {
            this.RaiseAndSetIfChanged(ref _allExpanded, value);
            if (value)
                ExpandAll();
            else
                CollapseAll();
        }
    }

    public Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?>
        ShowIssueDialog { get; } =
        new();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowUnpluggedDialog { get; } =
        new();

    public Interaction<ConfigViewModel, Unit> SaveConfig { get; } = new();
    public Interaction<ConfigViewModel, Unit> LoadConfig { get; } = new();

    public Interaction<(ConfigViewModel model, Output output),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; } = new();

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    public IEnumerable<DeviceControllerType> DeviceControllerRhythmTypes =>
        Enum.GetValues<DeviceControllerType>();

    public IEnumerable<ModeType> ModeTypes => Enum.GetValues<ModeType>();

    // Only Pico supports bluetooth
    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            Device.IsPico() ||
            type is not (EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse));

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();
    public IEnumerable<LegendType> LegendTypes => Enum.GetValues<LegendType>();

    //TODO: actually read and write this as part of the config
    public bool KvEnabled { get; set; } = false;
    public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
    public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

    public ICommand WriteConfigCommand { get; }

    public ICommand SaveConfigCommand { get; }
    public ICommand LoadConfigCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand GoBackCommand { get; }

    public string LocalAddress { get; }

    [Reactive] public MouseMovementType MouseMovementType { get; set; }

    [Reactive] public ModeType Mode { get; set; }

    [Reactive] public int Debounce { get; set; }

    private bool _deque;

    public bool Deque
    {
        get => _deque;
        set
        {
            this.RaiseAndSetIfChanged(ref _deque, value);
            if (value)
            {
                // If we have enabled deque, then make sure the poll rate and debounce are above the min
                PollRate = Math.Max(1, PollRate);
                Debounce = Math.Max(5, Debounce);
            }
            else
            {
                // If we have disabled deque, then round to the nearest whole debounce
                Debounce = (int) (Math.Round(Debounce / 10.0f) * 10);
            }
        }
    }

    [Reactive] public int StrumDebounce { get; set; }

    [Reactive] public int PollRate { get; set; }
    [Reactive] public int DjPollRate { get; set; }
    [Reactive] public bool DjDual { get; set; }
    [Reactive] public bool DjSmoothing { get; set; }

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

    [Reactive] public bool Connected { get; set; }


    public int UsbHostDm
    {
        get => _usbHostDm.Pin;
        set
        {
            _usbHostDm.Pin = value;
            _usbHostDp.Pin = value - 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDp));
            UpdateErrors();
        }
    }

    public int UsbHostDp
    {
        get => _usbHostDp.Pin;
        set
        {
            _usbHostDp.Pin = value;
            _usbHostDm.Pin = value + 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDm));
            UpdateErrors();
        }
    }

    [Reactive] public byte LedCount { get; set; }

    [Reactive] public byte WtSensitivity { get; set; }


    [Reactive] public bool HasError { get; set; }

    public LedType LedType
    {
        get => _ledType;
        set
        {
            if (value == LedType.None)
            {
                _apa102SpiConfig = null;
            }
            else if (_ledType == LedType.None)
            {
                _apa102SpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, false, -1, -1, -1, true, true,
                    true,
                    Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000));
                this.RaisePropertyChanged(nameof(Apa102Mosi));
                this.RaisePropertyChanged(nameof(Apa102Sck));
                UpdateErrors();
            }

            this.RaiseAndSetIfChanged(ref _ledType, value);
        }
    }

    [Reactive] public bool XInputOnWindows { get; set; }

    [Reactive] public LegendType LegendType { get; set; } = LegendType.Xbox;


    public DeviceControllerType DeviceControllerType
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
        set => _ = SetDefaultBindingsAsync(value);
    }

    public Microcontroller Microcontroller { get; private set; }

    public SourceList<Output> Bindings { get; } = new();
    public bool BindableSpi => IsPico;

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
            });
    } // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsStandardMode { get; }
    [ObservableAsProperty] public bool IsAdvancedMode { get; }
    [ObservableAsProperty] public bool IsGuitar { get; }
    [ObservableAsProperty] public bool IsStageKit { get; }
    [ObservableAsProperty] public bool IsController { get; }
    [ObservableAsProperty] public bool IsKeyboard { get; }
    [ObservableAsProperty] public bool IsApa102 { get; }
    [ObservableAsProperty] public bool IsBluetooth { get; }

    [ObservableAsProperty] public string? WriteToolTip { get; }

    [ObservableAsProperty] public string? PollRateLabel { get; }

    public bool UsbHostEnabled => Bindings.Items.Any(x =>
        x is UsbHostCombinedOutput ||
        x.Outputs.Items.Any(x2 => x2.Input.InnermostInput().InputType is InputType.UsbHostInput));

    // ReSharper enable UnassignedGetOnlyAutoProperty

    private static readonly Dictionary<object, int> TypeOrder =
        Enum.GetValues<InstrumentButtonType>().Cast<object>()
            .Concat(Enum.GetValues<DjInputType>().Cast<object>())
            .Concat(Enum.GetValues<StandardButtonType>().Cast<object>())
            .Concat(Enum.GetValues<DrumAxisType>().Cast<object>())
            .Concat(Enum.GetValues<GuitarAxisType>().Cast<object>())
            .Concat(Enum.GetValues<DjAxisType>().Cast<object>())
            .Concat(Enum.GetValues<Ps3AxisType>().Cast<object>())
            .Concat(Enum.GetValues<StandardAxisType>().Cast<object>()).Select((s, index) => new {s, index})
            .ToDictionary(x => x.s, x => x.index);

    public List<int> AvailableApaMosiPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaSckPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailablePins => Microcontroller.GetAllPins(false);

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => AvailablePins.Skip(1).ToList();
    public List<int> AvailablePinsDp => AvailablePins.SkipLast(1).ToList();

    public IEnumerable<PinConfig> PinConfigs =>
        new PinConfig?[] {_apa102SpiConfig, _usbHostDm, _usbHostDp, _unoRx, _unoTx}.Where(s => s != null)
            .Cast<PinConfig>();

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

    public IScreen HostScreen { get; }
    public bool IsPico => Device.IsPico();

    public void SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceControllerType type, EmulationType emulationType)
    {
        this.RaiseAndSetIfChanged(ref _deviceControllerType, type, nameof(DeviceControllerType));
        this.RaiseAndSetIfChanged(ref _emulationType, emulationType, nameof(EmulationType));
    }

    public void UpdateBindings()
    {
        foreach (var binding in Bindings.Items) binding.UpdateBindings();
        InstrumentButtonTypeExtensions.ConvertBindings(Bindings, this, false);
        if (!IsGuitar)
        {
            Deque = false;
        }

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, Bindings.Items);
        Bindings.RemoveMany(extra);

        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Items.Any(s =>
                s is WiiCombinedOutput or Ps2CombinedOutput or UsbHostCombinedOutput or BluetoothOutput)) return;


        if (_deviceControllerType == DeviceControllerType.Turntable)
        {
            if (!Bindings.Items.Any(s => s is DjCombinedOutput))
            {
                var dj = new DjCombinedOutput(this);
                dj.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(dj);
            }
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DjCombinedOutput));
        }

        if (!_deviceControllerType.Is5FretGuitar() && _deviceControllerType.IsDrum())
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (_deviceControllerType is DeviceControllerType.Turntable)
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode
            {
                Type: EmulationModeType.Ps4Or5 or EmulationModeType.XboxOne
            }));

        if (_deviceControllerType.IsDrum())
        {
            IEnumerable<DrumAxisType> difference =
                DrumAxisTypeMethods.GetDifferenceFor(_deviceControllerType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis));
        }

        if (_deviceControllerType.IsGuitar())
        {
            IEnumerable<GuitarAxisType> difference = GuitarAxisTypeMethods
                .GetDifferenceFor(_deviceControllerType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis));
        }

        if (_deviceControllerType is not DeviceControllerType.RockBandGuitar)
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (!_deviceControllerType.IsGuitar())
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarButton));

        foreach (var type in types)
            switch (type)
            {
                case StandardButtonType buttonType:
                    Bindings.Add(new ControllerButton(this,
                        new DirectInput(-1, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case InstrumentButtonType buttonType:
                    Bindings.Add(new GuitarButton(this,
                        new DirectInput(-1, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, ushort.MaxValue, axisType, false));
                    break;
                case GuitarAxisType.Slider:
                    Bindings.Add(new GuitarAxis(this, new GhWtTapInput(GhWtInputType.TapAll, this,
                            -1,
                            -1, -1,
                            -1),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, GuitarAxisType.Slider, false));
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, new DirectInput(-1,
                            false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, axisType, false));
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, 10, axisType, false));
                    break;
                case DjAxisType.EffectsKnob:
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, DjAxisType.EffectsKnob,
                        false));
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue, 0, axisType,
                        false));
                    break;
            }

        Bindings.Edit(s =>
        {
            var sorted = s.OrderBy(s2 => TypeOrder.GetValueOrDefault(s2.GetOutputType())).ToList();
            s.Clear();
            s.AddRange(sorted);
        });
    }

    public void SetDefaults()
    {
        Main.Message = "Building";
        Main.Progress = 0;
        ClearOutputs();
        Deque = false;
        LedType = LedType.None;
        _deviceControllerType = DeviceControllerType.Gamepad;
        CombinedStrumDebounce = false;
        WtSensitivity = 30;
        PollRate = 0;
        StrumDebounce = 0;
        Debounce = 10;
        DjPollRate = 4;
        SwapSwitchFaceButtons = false;

        this.RaisePropertyChanged(nameof(DeviceControllerType));
        this.RaisePropertyChanged(nameof(EmulationType));
        XInputOnWindows = true;
        MouseMovementType = MouseMovementType.Relative;

        switch (Main.DeviceInputType)
        {
            case DeviceInputType.Direct:
                _ = SetDefaultBindingsAsync(EmulationType);
                break;
            case DeviceInputType.Wii:
                var output = new WiiCombinedOutput(this)
                {
                    Expanded = true
                };
                output.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(output);
                break;
            case DeviceInputType.Ps2:
                var ps2Output = new Ps2CombinedOutput(this)
                {
                    Expanded = true
                };
                ps2Output.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(ps2Output);
                break;
            case DeviceInputType.Usb:
                var usbOutput = new UsbHostCombinedOutput(this)
                {
                    Expanded = true
                };
                usbOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(usbOutput);
                break;
            case DeviceInputType.Bluetooth:
                var bluetoothOutput = new BluetoothOutput(this, "")
                {
                    Expanded = true
                };
                bluetoothOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(bluetoothOutput);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        UpdateBindings();
        UpdateErrors();
        // Write the full config, bluetooth has zero config so we can actually properly write it
        Main.Write(this);
    }

    private async Task SetDefaultBindingsAsync(EmulationType emulationType)
    {
        // If going from say bluetooth controller to standard controller, the pin bindings can stay
        if (GetSimpleEmulationTypeFor(EmulationType) == GetSimpleEmulationTypeFor(emulationType))
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            UpdateErrors();
            return;
        }

        if (Bindings.Items.Any())
        {
            var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
                "The following action will clear all your bindings, are you sure you want to do this?")).ToTask();
            if (!yesNo.Response)
            {
                var last = _emulationType;
                _emulationType = emulationType;
                this.RaisePropertyChanged(nameof(EmulationType));
                _emulationType = last;
                this.RaisePropertyChanged(nameof(EmulationType));
                return;
            }
        }

        _emulationType = emulationType;
        this.RaisePropertyChanged(nameof(EmulationType));
        ClearOutputs();

        if (GetSimpleEmulationType() is EmulationType.KeyboardMouse) return;

        foreach (var type in Enum.GetValues<StandardAxisType>())
        {
            if (!ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                    .Any()) continue;
            var isTrigger = type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
            Bindings.Add(new ControllerAxis(this,
                new DirectInput(-1, false, DevicePinMode.Analog, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), isTrigger ? ushort.MinValue : short.MinValue,
                isTrigger ? ushort.MaxValue : short.MaxValue, 0,
                ushort.MaxValue, type, false));
        }

        foreach (var type in Enum.GetValues<StandardButtonType>())
        {
            if (!ControllerEnumConverter.Convert(type, _deviceControllerType, LegendType, SwapSwitchFaceButtons)
                    .Any()) continue;
            Bindings.Add(new ControllerButton(this,
                new DirectInput(-1, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, type, false));
        }

        UpdateErrors();
    }

    public string Generate(MemoryStream? blobStream)
    {
        if (Device is Santroller santroller)
        {
            santroller.StopTicking();
        }

        BinaryWriter? writer = null;
        var outputs = Bindings.Items.SelectMany(binding => binding.Outputs.Items).ToList();
        var inputs = outputs.Select(binding => binding.Input.InnermostInput()).ToList();
        var directInputs = inputs.OfType<DirectInput>().ToList();
        string config;
        int configLength;
        using (var outputStream = new MemoryStream())
        {
            using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(this));
            }

            config =
                $"#define CONFIGURATION {{{string.Join(",", outputStream.ToArray().Select(b => "0x" + b.ToString("X")))}}}";
            config += "\n";
            configLength = outputStream.ToArray().Length;
        }

        if (blobStream != null)
        {
            writer = new BinaryWriter(blobStream);
            writer.Write((ushort) configLength);
            writer.Write((ushort) (SwapSwitchFaceButtons ? 1 : 0));
            writer.Write((ushort) (XInputOnWindows ? 1 : 0));
            writer.Write((ushort) (Deque ? 1 : 0));
            writer.Write((ushort) PollRate);
            writer.Write((ushort) DjPollRate);
            writer.Write((ushort) Debounce);
            writer.Write((ushort) StrumDebounce);
            writer.Write((ushort) WtSensitivity);
            config += """
                      #define CONFIGURABLE_BLOBS
                      #define CONFIGURATION_LEN config_blocks[0]
                      #define SWAP_SWITCH_FACE_BUTTONS config_blobs[1]
                      #define WINDOWS_USES_XINPUT config_blobs[2]
                      #define INPUT_QUEUE config_blobs[3]
                      #define POLL_RATE config_blobs[4]
                      #define WT_SENSITIVITY config_blobs[5]
                      #define INPUT_DJ_TURNTABLE_POLL_RATE config_blobs[6]
                      #define INPUT_DJ_TURNTABLE_SMOOTHING config_blobs[7]
                      #define INPUT_DJ_TURNTABLE_SMOOTHING_DUAL config_blobs[8]
                      """;
        }
        else
        {
            config += $"""
                       #define CONFIGURATION_LEN {configLength}
                       #define SWAP_SWITCH_FACE_BUTTONS {(!SwapSwitchFaceButtons).ToString().ToLower()}
                       #define WINDOWS_USES_XINPUT {XInputOnWindows.ToString().ToLower()}
                       #define INPUT_QUEUE {Deque.ToString().ToLower()}
                       #define POLL_RATE {PollRate}
                       #define WT_SENSITIVITY {WtSensitivity}
                       #define INPUT_DJ_TURNTABLE_POLL_RATE {DjPollRate * 1000}
                       #define INPUT_DJ_TURNTABLE_SMOOTHING {DjSmoothing.ToString().ToLower()}
                       #define INPUT_DJ_TURNTABLE_SMOOTHING_DUAL {DjDual.ToString().ToLower()}
                       """;
        }

        config += "\n";
        config += $"""
                   #define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}
                   #define ARDWIINO_BOARD "{Microcontroller.Board.ArdwiinoName}"
                   #define CONSOLE_TYPE {GetEmulationType()}
                   #define DEVICE_TYPE {(byte) DeviceControllerType}
                   """;

        // Actually write the config as configured
        if (!HasError)
        {
            // Sort by pin index, and then map to adc number and turn into an array
            var analogPins = directInputs.Where(s => s.IsAnalog).OrderBy(s => s.PinConfig.Pin)
                .Select(s => Microcontroller.GetChannel(s.PinConfig.Pin, false).ToString()).Distinct().ToList();
            config += "\n";
            config += $$"""
                        #define USB_HOST_STACK {{UsbHostEnabled.ToString().ToLower()}}
                        #define USB_HOST_DP_PIN {{UsbHostDp}}
                        #define DIGITAL_COUNT {{CalculateDebounceTicks()}}
                        #define LED_COUNT {{LedCount}}
                        #define LED_TYPE {{GetLedType()}}
                        #define ADC_PINS {{{string.Join(",", analogPins)}}}
                        #define ADC_COUNT {{analogPins.Count}}
                        #define TICK_SHARED \
                            {{GenerateTick(ConfigField.Shared, writer)}}
                        #define TICK_DETECTION \
                            {{GenerateTick(ConfigField.Detection, writer)}}
                        #define TICK_PS3 \
                            {{GenerateTick(ConfigField.Ps3, writer)}}
                        #define TICK_PS3_WITHOUT_CAPTURE \
                            {{GenerateTick(ConfigField.Ps3WithoutCapture, writer)}}
                        #define TICK_PC \
                            {{GenerateTick(ConfigField.Universal, writer)}}
                        #define TICK_PS4 \
                            {{GenerateTick(ConfigField.Ps4, writer)}}
                        #define TICK_XINPUT \
                            {{GenerateTick(ConfigField.Xbox360, writer)}}
                        #define TICK_XBOX_ONE \
                            {{GenerateTick(ConfigField.XboxOne, writer)}}
                        #define HANDLE_AUTH_LED \
                            {{GenerateTick(ConfigField.AuthLed, writer)}}
                        #define HANDLE_PLAYER_LED \
                            {{GenerateTick(ConfigField.PlayerLed, writer)}}
                        #define HANDLE_LIGHTBAR_LED \
                            {{GenerateTick(ConfigField.LightBarLed, writer)}}
                        #define HANDLE_RUMBLE \
                            {{GenerateTick(ConfigField.RumbleLed, writer)}}
                        #define HANDLE_KEYBOARD_LED \
                            {{GenerateTick(ConfigField.KeyboardLed, writer)}}
                        #define PIN_INIT \
                            {{GenerateInit()}}
                        #define LED_INIT \
                            {{GenerateTick(ConfigField.InitLed, writer)}}
                        """;

            var nkroTick = GenerateTick(ConfigField.Keyboard, writer);
            if (nkroTick.Any())
                config += $"""

                           #define TICK_NKRO \
                               {nkroTick}
                           """;

            var consumerTick = GenerateTick(ConfigField.Consumer, writer);
            if (consumerTick.Any())
                config += $"""

                           #define TICK_CONSUMER \
                               {consumerTick}
                           """;

            var mouseTick = GenerateTick(ConfigField.Mouse, writer);
            if (mouseTick.Any()) config += $"""

                                            #define TICK_MOUSE \
                                                {mouseTick}
                                            """;

            if (IsApa102)
            {
                config += $"""

                           #define {Apa102SpiType.ToUpper()}_SPI_PORT {_apa102SpiConfig!.Definition}
                           #define TICK_LED \
                               {GenerateLedTick()}
                           """;
            }


            var offLed = GenerateTick(ConfigField.OffLed, writer);
            if (offLed.Any()) config += $"""

                                         #define HANDLE_LED_RUMBLE_OFF \
                                             {offLed}
                                         """;
            if (EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
            {
                config += $"""

                           #define BLUETOOTH_TX {(EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse).ToString().ToLower()}
                           """;
            }

            if (KvEnabled)
            {
                config += $$"""

                            #define KV_KEY_1 {{{string.Join(",", KvKey1.ToArray().Select(b => "0x" + b.ToString("X")))}}}
                            #define KV_KEY_2 {{{string.Join(",", KvKey2.ToArray().Select(b => "0x" + b.ToString("X")))}}}
                            """;
            }

            config += $"""

                       {GetPinConfigs().Distinct().Aggregate("", (current, pinConfig) => current + pinConfig.Generate())}
                       {string.Join("\n", inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}"))}
                       """;
        }
        else
        {
            // Write an empty config - the config at this point is likely invalid and won't compile
            config += """

                      #define USB_HOST_STACK false
                      #define USB_HOST_DP_PIN 0
                      #define TICK_SHARED
                      #define TICK_DETECTION
                      #define TICK_PC
                      #define TICK_PS3
                      #define TICK_PS3_WITHOUT_CAPTURE
                      #define TICK_PS4
                      #define TICK_XINPUT
                      #define TICK_XBOX_ONE
                      #define DIGITAL_COUNT 0
                      #define LED_COUNT 0
                      #define LED_TYPE 0
                      #define INPUT_QUEUE false
                      #define HANDLE_AUTH_LED
                      #define HANDLE_PLAYER_LED
                      #define HANDLE_LIGHTBAR_LED
                      #define HANDLE_RUMBLE
                      #define HANDLE_KEYBOARD_LED
                      #define ADC_PINS {}
                      #define ADC_COUNT 0
                      #define PIN_INIT
                      #define LED_INIT
                      """;
        }

        if (blobStream != null)
        {
            blobStream.Seek(0, SeekOrigin.Begin);
            var blobLength = blobStream.Length;
            config += $$"""

                        #define CONFIGURABLE_BLOBS {{{blobStream.ToArray().Select(b => "0x" + b.ToString("X"))}}}
                        #define CONFIGURABLE_BLOBS_LEN {{blobLength}}
                        """;
        }

        return config;
    }

    private string GenerateInit()
    {
        return FixNewlines(Microcontroller.GenerateInit(this));
    }

    public PinConfig[] UsbHostPinConfigs()
    {
        return UsbHostEnabled ? new PinConfig[] {_usbHostDm, _usbHostDp} : Array.Empty<PinConfig>();
    }

    private byte GetEmulationType()
    {
        return (byte) GetSimpleEmulationType();
    }

    private EmulationType GetSimpleEmulationTypeFor(EmulationType type)
    {
        switch (type)
        {
            case EmulationType.Bluetooth:
            case EmulationType.Controller:
                return EmulationType.Controller;
            case EmulationType.KeyboardMouse:
            case EmulationType.BluetoothKeyboardMouse:
                return EmulationType.KeyboardMouse;
            default:
                return EmulationType;
        }
    }

    public EmulationType GetSimpleEmulationType()
    {
        return GetSimpleEmulationTypeFor(EmulationType);
    }

    private int GetLedType()
    {
        switch (LedType)
        {
            case LedType.Apa102Rgb:
            case LedType.Apa102Rbg:
            case LedType.Apa102Grb:
            case LedType.Apa102Gbr:
            case LedType.Apa102Brg:
            case LedType.Apa102Bgr:
                return 1;
            case LedType.None:
            default:
                return 0;
        }
    }

    private async Task BindAllAsync()
    {
        foreach (var binding in Bindings.Items)
        {
            if (binding.Input.InnermostInput() is not DirectInput) continue;
            var response = await ShowBindAllDialog.Handle((this, binding)).ToTask();
            if (!response.Response) return;
        }
    }

    public void RemoveOutput(Output output)
    {
        if (Bindings.Remove(output))
        {
            UpdateErrors();
            return;
        }

        foreach (var binding in Bindings.Items) binding.Outputs.Remove(output);

        UpdateErrors();
    }

    [RelayCommand]
    public void ClearOutputs()
    {
        Bindings.Clear();
        UpdateErrors();
    }

    public async Task ClearOutputsWithConfirmationAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        Bindings.Clear();
        UpdateErrors();
    }

    [RelayCommand]
    public void ExpandAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = true;
    }

    [RelayCommand]
    public void CollapseAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = false;
    }

    [RelayCommand]
    public async Task ResetWithConfirmationAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Load Defaults", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        Bindings.Clear();
        UpdateBindings();
        UpdateErrors();
    }

    private async Task ResetAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Revert", "Cancel",
                "The following action will revert your device back to an Arduino, are you sure you want to do this?"))
            .ToTask();
        if (!yesNo.Response) return;
        if (Device is not Santroller device) return;
        await Main.RevertCommand.Execute(device);
    }

    [RelayCommand]
    public void AddOutput()
    {
        if (IsController)
            Bindings.Add(new EmptyOutput(this));
        else if (IsKeyboard)
            Bindings.Add(new KeyboardButton(this, new DirectInput(0, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, Key.Space));

        UpdateErrors();
    }

    private static string FixNewlines(string code)
    {
        return NewlineRegex().Replace(code.Replace("\r", "").Trim(), "").Replace("\n", "\\\n    ");
    }

    private string GenerateLedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (_ledType == LedType.None ||
            !outputs.Any(s => s.LedIndices.Any())) return "";
        var ledMax = outputs.SelectMany(output => output.LedIndices).Max();
        var ret =
            """
            
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            spi_transfer(APA102_SPI_PORT, 0x00);
            """;
        for (var i = 0; i < ledMax; i++)
        {
            ret +=
                $"""
                 
                 spi_transfer(APA102_SPI_PORT, 0xff);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].r);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].g);
                 spi_transfer(APA102_SPI_PORT, ledState[{i}].b);
                 """;
        }

        for (var i = 0; i <= ledMax; i += 16)
        {
            ret += """

                   spi_transfer(APA102_SPI_PORT, 0xff);
                   """;
        }
        return GenerateTick(ConfigField.StrobeLed, null).Trim() + FixNewlines(ret);
    }

    private string GenerateTick(ConfigField mode, BinaryWriter? writer)
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInput().GetType()).ToList();
        var combined = DeviceControllerType.IsGuitar() && CombinedStrumDebounce;
        Dictionary<string, int> debounces = new();
        var strumIndices = new List<int>();

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new Dictionary<string, List<(int, Input)>>();
        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;

                if (output.Input is MacroInput)
                {
                    foreach (var input in output.Input.Inputs())
                    {
                        var gen = input.Generate();
                        macros.TryAdd(gen, new List<(int, Input)>());
                        macros[gen].AddRange(output.Input.Inputs().Where(s => s != input).Select(s => (0, s)));
                    }
                }

                debounces.TryAdd(generatedInput, debounces.Count);

                if (combined && output is GuitarButton
                    {
                        IsStrum: true
                    })
                    strumIndices.Add(debounces[generatedInput]);

                if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

                inputs[generatedInput].Add(debounces[generatedInput]);
            }
        }

        foreach (var (key, value) in macros)
        {
            var list2 = new List<(int, Input)>();
            foreach (var (_, input) in value)
            {
                var gen = input.Generate();
                if (debounces.TryGetValue(gen, out var debounce))
                {
                    list2.Add((debounce, input));
                }

                macros[key] = list2;
            }
        }

        var debouncesRelatedToLed = new Dictionary<byte, List<(Output, int)>>();
        var analogRelatedToLed = new Dictionary<byte, List<OutputAxis>>();
        // Handle most mappings
        var ret = outputsByType
            .Aggregate("", (current, group) =>
            {
                // we need to ensure that DigitalToAnalog is last
                return current + group
                    .First().Input.InnermostInput()
                    .GenerateAll(group
                        // DigitalToAnalog and MacroInput need to be handled last
                        .OrderByDescending(s => s.Input is DigitalToAnalog or MacroInput ? 0 : 1)
                        .Select(s =>
                        {
                            var input = s.Input;
                            var output = s;
                            var generatedInput = input.Generate();
                            var index = 0;
                            if (output is OutputButton or DrumAxis or EmulationMode)
                            {
                                index = debounces[generatedInput];

                                foreach (var led in output.LedIndices)
                                {
                                    if (!debouncesRelatedToLed.ContainsKey(led))
                                        debouncesRelatedToLed[led] = new List<(Output, int)>();

                                    debouncesRelatedToLed[led].Add((output, index));
                                }
                            }

                            if (output is OutputAxis axis)
                            {
                                foreach (var led in output.LedIndices)
                                {
                                    if (!analogRelatedToLed.ContainsKey(led))
                                        analogRelatedToLed[led] = new List<OutputAxis>();

                                    analogRelatedToLed[led].Add(axis);
                                }
                            }

                            var generated = output.Generate(mode, index, "", "", strumIndices, macros, writer);
                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .Distinct().ToList(), mode);
            });
        if (mode == ConfigField.Shared && LedType is not LedType.None)
        {
            // Handle leds, including when multiple leds are assigned to a single output.
            foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
            {
                var analog = "";
                if (analogRelatedToLed.TryGetValue(led, out var analogLedOutputs))
                {
                    foreach (var analogLedOutput in analogLedOutputs)
                    {
                        var ledRead =
                            analogLedOutput.GenerateAssignment("0", ConfigField.Ps3, false, true, false, null);
                        // Now we have the value, calibrated as a uint8_t
                        // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                        analog +=
                            $$"""
                              led_tmp = {{ledRead}};
                              if(led_tmp) {
                                  {{LedType.GetLedAssignment(led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp")}}
                              } else {
                                  {{LedType.GetLedAssignment(relatedOutputs.First().Item1.LedOff, led)}}
                              }
                              """;
                    }
                }

                if (!analog.Any())
                {
                    analog = LedType.GetLedAssignment(relatedOutputs.First().Item1.LedOff, led);
                }

                ret += $"if (ledState[{led - 1}].select == 0) {{";
                ret += string.Join(" else ", relatedOutputs.Select(tuple =>
                {
                    var ifStatement = $"debounce[{tuple.Item2}]";
                    return $$"""
                             if ({{ifStatement}}) {
                                 {{LedType.GetLedAssignment(tuple.Item1.LedOn, led)}}
                             }
                             """;
                }));
                ret += $$"""
                             else {
                                 {{analog}}
                             }
                         }
                         """;
            }

            foreach (var (led, analogLedOutputs) in analogRelatedToLed)
            {
                if (debouncesRelatedToLed.ContainsKey(led)) continue;
                ret += $"if (ledState[{led - 1}].select == 0) {{";
                foreach (var analogLedOutput in analogLedOutputs)
                {
                    var ledRead = analogLedOutput.GenerateAssignment("0", ConfigField.Ps3, false, true, false, writer);
                    // Now we have the value, calibrated as a uint8_t
                    ret +=
                        $"led_tmp = {ledRead};{LedType.GetLedAssignment(led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp")}";
                }

                ret += "}";
            }
        }

        return FixNewlines(ret);
    }

    private int CalculateDebounceTicks()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInput().GetType()).ToList();
        Dictionary<string, int> debounces = new();

        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;


                debounces.TryAdd(generatedInput, debounces.Count);
            }
        }

        return debounces.Count;
    }

    public List<PinConfig> GetPinConfigs()
    {
        return Bindings.Items.SelectMany(s => s.GetPinConfigs()).Concat(PinConfigs).Distinct().ToList();
    }

    public Dictionary<string, List<int>> GetPins(string type)
    {
        var pins = new Dictionary<string, List<int>>();
        foreach (var binding in Bindings.Items)
        {
            var configs = binding.GetPinConfigs();
            //Exclude digital or analog pins (which use a guid containing a -)
            if (configs.Any(s => s.Type == type || (type.Contains("-") && s.Type.Contains("-")))) continue;
            if (!pins.ContainsKey(binding.LocalisedName)) pins[binding.LocalisedName] = new List<int>();

            foreach (var pinConfig in configs) pins[binding.LocalisedName].AddRange(pinConfig.Pins);
        }

        if (Main.IsUno || Main.IsMega)
        {
            pins[UnoPinTypeTx] = new List<int> {UnoPinTypeTxPin};
            pins[UnoPinTypeRx] = new List<int> {UnoPinTypeRxPin};
        }

        if (IsApa102 && _apa102SpiConfig != null && type != Apa102SpiType)
            pins["APA102"] = _apa102SpiConfig.Pins.ToList();

        if (UsbHostEnabled && type != UsbHostPinTypeDm && type != UsbHostPinTypeDp)
            pins["USB Host"] = new List<int> {UsbHostDm, UsbHostDp};

        return pins;
    }

    public void UpdateErrors()
    {
        var foundError = false;
        foreach (var output in Bindings.Items)
        {
            output.UpdateErrors();
            if (!string.IsNullOrEmpty(output.ErrorText)) foundError = true;
        }

        if (IsApa102 && Microcontroller.SpiAssignable)
        {
            var error = _apa102SpiConfig!.ErrorText;
            if (error != null)
            {
                foundError = true;
            }

            Apa102ErrorText = error;
        }
        else
        {
            Apa102ErrorText = "";
        }

        HasError = foundError;
    }

    public void AddDevice(IConfigurableDevice device)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Trace.WriteLine($"Add called, current device: {Device},  new device: {device}");
            Trace.Flush();
            if (device is Santroller santroller)
            {
                Main.Complete(100);
                Device = device;
                Microcontroller = device.GetMicrocontroller(this);
                Main.SetDifference(false);
                santroller.LoadConfiguration(this);
            }

            Device.DeviceAdded(device);
        });
    }

    public void GoBack()
    {
        if (Device is Santroller santroller)
        {
            santroller.StopTicking();
        }

        Main.SetDifference(false);
        Main.GoBack.Execute();
    }

    public bool UsingBluetooth()
    {
        return IsBluetooth || Bindings.Items.Any(s => s is BluetoothOutput);
    }

    public void RemoveDevice(IConfigurableDevice device)
    {
        if (!Main.Working && Device is Santroller old &&
            (device.IsSameDevice(old.Serial) || device.IsSameDevice(old.Path)))
        {
            old.StopTicking();
            Main.SetDifference(false);
            ShowUnpluggedDialog.Handle(("", "", "")).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => Main.GoBack.Execute(new Unit()));
        }
    }

    public void Update(byte[] btRaw)
    {
        if (IsBluetooth && btRaw.Any())
        {
            Connected = btRaw[0] != 0;
        }
    }

    public TwiConfig? GetTwiForType(string twiType)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<TwiConfig>().FirstOrDefault(s => s.Type == twiType))
            .FirstOrDefault(found => found != null);
    }

    public SpiConfig? GetSpiForType(string spiType)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<SpiConfig>().FirstOrDefault(s => s.Type == spiType))
            .FirstOrDefault(found => found != null);
    }

    public DirectPinConfig GetPinForType(string pinType, int fallbackPin, DevicePinMode fallbackMode)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<DirectPinConfig>().FirstOrDefault(s => s.Type == pinType))
            .FirstOrDefault(found => found != null) ?? new DirectPinConfig(this, pinType, fallbackPin, fallbackMode);
    }

    public void MoveUp(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index - 1);
    }

    public void MoveDown(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index + 1);
    }

    // Capture any input events (such as pointer or keyboard) - used for detecting the last input
    public readonly Subject<object> KeyOrPointerEvent = new();

    public void OnKeyEvent(KeyEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerUpdateKind args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(Point args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    [GeneratedRegex("^\\s+$[\\r\\n]*", RegexOptions.Multiline)]
    private static partial Regex NewlineRegex();
}