using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class LedIndex : ReactiveObject
{
    public LedIndex(Output output, byte i)
    {
        Output = output;
        Index = i;
    }

    public Output Output { get; }
    public byte Index { get; }

    public bool Selected
    {
        get => Output.LedIndices.Contains(Index);
        set
        {
            if (value)
                Output.LedIndices.Add(Index);
            else
                Output.LedIndices.Remove(Index);

            this.RaisePropertyChanged();
        }
    }
}

public abstract partial class Output : ReactiveObject
{
    private readonly Guid _id = new();
    protected readonly ConfigViewModel Model;

    private readonly bool _configured;
    private Color _ledOff;

    private Color _ledOn;

    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }


    protected Output(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        bool childOfCombined)
    {
        ChildOfCombined = childOfCombined;
        ButtonText = "Click to assign";
        Model = model;
        Input = input;
        LedIndices = new ObservableCollection<byte>(ledIndices);
        LedOn = ledOn;
        LedOff = ledOff;
        MoveUp = ReactiveCommand.Create(() => Model.MoveUp(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != 0));
        MoveDown = ReactiveCommand.Create(() => Model.MoveDown(this),
            Model.Bindings.Connect().Select(_ => Model.Bindings.Items.IndexOf(this) != Model.Bindings.Count - 1));
        this.WhenAnyValue(x => x.Model.LedCount)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, (byte) s)).ToArray())
            .ToPropertyEx(this, x => x.AvailableIndices);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInput() is DjInput)
            .ToPropertyEx(this, x => x.IsDj);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInput() is UsbHostInput)
            .ToPropertyEx(this, x => x.IsUsb);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInput() is WiiInput)
            .ToPropertyEx(this, x => x.IsWii);
        this.WhenAnyValue(x => x.Input)
            .Select(x => x.InnermostInput() is Gh5NeckInput && this is not GuitarAxis)
            .ToPropertyEx(this, x => x.IsGh5);
        this.WhenAnyValue(x => x.Input).Select(x => x.InnermostInput() is Ps2Input)
            .ToPropertyEx(this, x => x.IsPs2);
        this.WhenAnyValue(x => x.Input)
            .Select(x => x.InnermostInput() is GhWtTapInput && this is not GuitarAxis)
            .ToPropertyEx(this, x => x.IsWt);
        this.WhenAnyValue(x => x.Input.Title, x => x.Model.DeviceType, x => x.Model.RhythmType,
                x => x.ShouldUpdateDetails)
            .Select(x => $"{x.Item1} ({GetName(x.Item2, x.Item3)})")
            .ToPropertyEx(this, x => x.Title);
        this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not LedType.None)
            .ToPropertyEx(this, x => x.AreLedsEnabled);
        this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.RhythmType, x => x.ShouldUpdateDetails)
            .Select(x => GetName(x.Item1, x.Item2))
            .ToPropertyEx(this, x => x.LocalisedName);
        this.WhenAnyValue(x => x.Input!.RawValue, x => x.Enabled).Select(x => x.Item2 ? x.Item1 : 0)
            .ToPropertyEx(this, x => x.ValueRaw);
        this.WhenAnyValue(x => x.ValueRaw, x => x.Input, x => x.IsCombined)
            .Select(s => s.Item3 || s.Item2.IsAnalog ? 1 : (s.Item1 == 0 ? 0 : 0.35) + 0.65)
            .ToPropertyEx(this, s => s.ImageOpacity);
        this.WhenAnyValue(x => x.Enabled)
            .Select(s => s ? 1 : 0.5)
            .ToPropertyEx(this, s => s.CombinedOpacity);
        this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.RhythmType, x => x.ShouldUpdateDetails,
                x => x.ChildOfCombined)
            .Select(x => x.Item4 ? GetChildOutputType() : GetOutputType())
            .ToPropertyEx(this, x => x.OutputType);
        this.WhenAnyValue(x => x.Enabled)
            .Select(enabled => enabled ? Brush.Parse("#99000000") : Brush.Parse("#33000000"))
            .ToPropertyEx(this, s => s.CombinedBackground);
        Outputs = new SourceList<Output>();
        Outputs.Add(this);
        AnalogOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        DigitalOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        _configured = true;
    }


    public virtual bool LedsRequireColours => true;

    private bool ShouldUpdateDetails { get; set; }

    [Reactive] public Input Input { get; set; }


    [Reactive] public bool Enabled { get; set; } = true;

    [Reactive] public bool Expanded { get; set; }

    public Color LedOn
    {
        get => _ledOn;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledOn, value);
            if (!_configured || Model.LedType is LedType.None) return;
            if (Model.Device is Santroller santroller)
                foreach (var ledIndex in LedIndices)
                    santroller.SetLed((byte) (ledIndex - 1), Model.LedType.GetLedBytes(value));
        }
    }

    public Color LedOff
    {
        get => _ledOff;
        set
        {
            this.RaiseAndSetIfChanged(ref _ledOff, value);
            if (!_configured || Model.LedType is LedType.None) return;
            if (Model.Device is Santroller santroller)
                foreach (var ledIndex in LedIndices)
                    santroller.SetLed((byte) (ledIndex - 1), Model.LedType.GetLedBytes(value));
        }
    }


    public InputType? SelectedInputType
    {
        get => Input.InputType;
        set => SetInput(value, null, null, null, null, null, null);
    }

    public WiiInputType WiiInputType
    {
        get => (Input.InnermostInput() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType, value, null, null, null, null, null);
    }

    public Ps2InputType Ps2InputType
    {
        get => (Input.InnermostInput() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType, null, value, null, null, null, null);
    }

    public object KeyOrMouse
    {
        get => GetKey() ?? Key.Space;
        set => SetKey(value);
    }

    public DjInputType DjInputType
    {
        get => (Input.InnermostInput() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType, null, null, null, null, value, null);
    }

    public UsbHostInputType UsbInputType
    {
        get => (Input.InnermostInput() as UsbHostInput)?.Input ?? UsbHostInputType.A;
        set => SetInput(SelectedInputType, null, null, null, null, null, value);
    }

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => (Input.InnermostInput() as Gh5NeckInput)?.Input ?? Gh5NeckInputType.Green;
        set => SetInput(SelectedInputType, null, null, null, value, null, null);
    }

    public GhWtInputType GhWtInputType
    {
        get => (Input.InnermostInput() as GhWtTapInput)?.Input ?? GhWtInputType.TapGreen;
        set => SetInput(SelectedInputType, null, null, value, null, null, null);
    }

    public IEnumerable<GhWtInputType> GhWtInputTypes => Enum.GetValues<GhWtInputType>();

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes => Enum.GetValues<Gh5NeckInputType>();
    public IEnumerable<UsbHostInputType> UsbInputTypes => Enum.GetValues<UsbHostInputType>();

    public IEnumerable<object> KeyOrMouseInputs => Enum.GetValues<MouseButtonType>().Cast<object>()
        .Concat(Enum.GetValues<MouseAxisType>().Cast<object>()).Concat(KeyboardButton.Keys.Keys.Cast<object>());

    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes =>
        Enum.GetValues<WiiInputType>().OrderBy(s => EnumToStringConverter.Convert(s));

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();

    public IEnumerable<InputType> InputTypes =>
        Enum.GetValues<InputType>().Where(s =>
            (this is not GuitarAxis {Type: GuitarAxisType.Slider} ||
             s is InputType.Gh5NeckInput or InputType.WtNeckInput) &&
            (s is not InputType.MultiplexerInput || Model.IsPico) &&
            (s is not InputType.MacroInput || this is OutputButton) && s is not InputType.RfInput &&
            s is not InputType.UsbHostInput || Model.IsPico);

    private object GetChildOutputType()
    {
        if (Input.InnermostInput() is WiiInput wii) return wii.Input;

        if (Input.InnermostInput() is Ps2Input ps2) return ps2.Input;

        if (Input.InnermostInput() is DjInput dj) return dj.Input;

        if (Input.InnermostInput() is Gh5NeckInput gh5) return gh5.Input;

        if (Input.InnermostInput() is GhWtTapInput wt) return wt.Input;
        
        if (Input.InnermostInput() is UsbHostInput usb) return usb.Input;

        return GetOutputType();
    }

    protected void UpdateDetails()
    {
        ShouldUpdateDetails = true;
        this.RaisePropertyChanged(nameof(ShouldUpdateDetails));
        ShouldUpdateDetails = false;
        this.RaisePropertyChanged(nameof(ShouldUpdateDetails));
    } // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public object? OutputType { get; }
    [ObservableAsProperty] public string LocalisedName { get; } = "";
    [ObservableAsProperty] public bool IsDj { get; }
    [ObservableAsProperty] public bool IsWii { get; }
    [ObservableAsProperty] public bool IsUsb { get; }
    [ObservableAsProperty] public bool IsPs2 { get; }
    [ObservableAsProperty] public bool IsGh5 { get; }
    [ObservableAsProperty] public bool IsWt { get; }
    [ObservableAsProperty] public bool AreLedsEnabled { get; }
    [ObservableAsProperty] public LedIndex[] AvailableIndices { get; } = Array.Empty<LedIndex>();

    [ObservableAsProperty] public double CombinedOpacity { get; }
    [ObservableAsProperty] public IBrush CombinedBackground { get; } = Brush.Parse("#99000000");

    [ObservableAsProperty] public double ImageOpacity { get; }

    [ObservableAsProperty] public int ValueRaw { get; }
    [ObservableAsProperty] public string Title { get; } = "";

    // ReSharper enable UnassignedGetOnlyAutoProperty

    public abstract bool IsCombined { get; }
    public ObservableCollection<byte> LedIndices { get; set; }
    public string Id => _id.ToString();


    public abstract bool IsStrum { get; }

    public SourceList<Output> Outputs { get; }

    public ReadOnlyObservableCollection<Output> AnalogOutputs { get; protected set; }
    public ReadOnlyObservableCollection<Output> DigitalOutputs { get; protected set; }

    public abstract bool IsKeyboard { get; }
    public bool IsLed => this is Led;

    public bool ChildOfCombined { get; }
    public bool IsEmpty => this is EmptyOutput;

    [Reactive] public string ButtonText { get; set; }

    public virtual string ErrorText
    {
        get
        {
            var text = string.Join(", ",
                GetPinConfigs().Select(s => s.ErrorText).Distinct().Where(s => !string.IsNullOrEmpty(s)));
            if (text.Contains("missing")) return "Pin configuration missing!";
            return string.IsNullOrEmpty(text) ? "" : $"* Error: Conflicting pins: {text}!";
        }
    }


    public abstract string LedOnLabel { get; }

    public abstract string LedOffLabel { get; }

    public virtual bool SupportsLedOff => true;
    public bool ConfigurableInput => Input is not (FixedInput or MacroInput);

    private object? GetKey()
    {
        switch (this)
        {
            case KeyboardButton button:
                return button.Key;
            case MouseAxis axis:
                return axis.Type;
            case MouseButton button:
                return button.Type;
        }

        return null;
    }

    private void SetKey(object value)
    {
        var current = GetKey();
        if (current == null) return;
        if (current.GetType() == value.GetType() && (int) current == (int) value) return;

        byte debounce = 1;
        int min = short.MinValue;
        int max = short.MaxValue;
        var deadzone = 0;
        switch (this)
        {
            case OutputAxis axis:
                min = axis.Min;
                max = axis.Max;
                deadzone = axis.DeadZone;
                break;
            case OutputButton button:
                debounce = button.Debounce;
                break;
        }

        Output? newOutput = value switch
        {
            Key key => new KeyboardButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), debounce, key),
            MouseButtonType mouseButtonType => new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(),
                debounce, mouseButtonType),
            MouseAxisType axisType => new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(), min, max,
                deadzone, axisType),
            _ => null
        };

        if (newOutput == null) return;
        newOutput.Expanded = Expanded;
        Model.Bindings.Insert(Model.Bindings.Items.IndexOf(this), newOutput);
        Model.RemoveOutput(this);
    }

    public abstract string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType);

    public abstract object GetOutputType();

    public static string GetReportField(object type, string field = "report")
    {
        var typeName = type.ToString()!;
        return $"{field}->{char.ToLower(typeName[0])}{typeName[1..]}";
    }

    [RelayCommand]
    private async Task FindAndAssignAsync()
    {
        ButtonText = "Move the mouse or click / press any key to use that input";
        var lastEvent = await Model.KeyOrPointerEvent.Take(1).ToTask();
        switch (lastEvent)
        {
            case KeyEventArgs keyEventArgs:
                KeyOrMouse = keyEventArgs.Key;
                break;
            case PointerUpdateKind pointerUpdateKind:
                switch (pointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonPressed:
                    case PointerUpdateKind.LeftButtonReleased:
                        KeyOrMouse = MouseButtonType.Left;
                        break;
                    case PointerUpdateKind.MiddleButtonPressed:
                    case PointerUpdateKind.MiddleButtonReleased:
                        KeyOrMouse = MouseButtonType.Middle;
                        break;
                    case PointerUpdateKind.RightButtonPressed:
                    case PointerUpdateKind.RightButtonReleased:
                        KeyOrMouse = MouseButtonType.Right;
                        break;
                }

                break;
            case PointerWheelEventArgs wheelEventArgs:
                KeyOrMouse = Math.Abs(wheelEventArgs.Delta.X) > Math.Abs(wheelEventArgs.Delta.Y)
                    ? MouseAxisType.ScrollX
                    : MouseAxisType.ScrollY;
                break;
            case Point point:
                await Task.Delay(100);
                while (true)
                {
                    var last = await Model.KeyOrPointerEvent.Take(1).ToTask();
                    if (last is not Point lastPoint) continue;
                    var diff = lastPoint - point;
                    KeyOrMouse = Math.Abs(diff.X) > Math.Abs(diff.Y) ? MouseAxisType.X : MouseAxisType.Y;
                    break;
                }
                break;
        }

        ButtonText = "Click to assign";
    }

    private void SetInput(InputType? inputType, WiiInputType? wiiInput, Ps2InputType? ps2InputType,
        GhWtInputType? ghWtInputType, Gh5NeckInputType? gh5NeckInputType, DjInputType? djInputType,
        UsbHostInputType? usbInputType)
    {
        Input input;
        switch (inputType)
        {
            case InputType.UsbHostInput when Input.InnermostInput() is not UsbHostInput:
                usbInputType ??= UsbHostInputType.A;
                input = new UsbHostInput(usbInputType.Value, Model);
                break;
            case InputType.UsbHostInput when Input.InnermostInput() is UsbHostInput usbHost:
                usbInputType ??= usbHost.Input;
                input = new UsbHostInput(usbInputType.Value, Model);
                break;
            case InputType.AnalogPinInput:
                input = new DirectInput(-1, false, DevicePinMode.Analog, Model);
                break;
            case InputType.MultiplexerInput:
                input = new MultiplexerInput(-1, 0, -1, -1, -1, -1, MultiplexerType.EightChannel, Model);
                break;
            case InputType.MacroInput:
                input = new MacroInput(new DirectInput(-1, false,DevicePinMode.PullUp, Model),
                    new DirectInput(-1, false,DevicePinMode.PullUp, Model), Model);
                break;
            case InputType.DigitalPinInput:
                input = new DirectInput(-1, false,DevicePinMode.PullUp, Model);
                break;
            case InputType.TurntableInput when Input.InnermostInput() is not DjInput:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model, true);
                break;
            case InputType.TurntableInput when Input.InnermostInput() is DjInput dj:
                djInputType ??= dj.Input;
                input = new DjInput(djInputType.Value, Model, dj.Smoothing, dj.Sda, dj.Scl);
                break;
            case InputType.Gh5NeckInput when Input.InnermostInput() is not Gh5NeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                if (this is GuitarAxis) gh5NeckInputType = Gh5NeckInputType.TapBar;

                input = new Gh5NeckInput(gh5NeckInputType.Value, Model);
                break;
            case InputType.Gh5NeckInput when Input.InnermostInput() is Gh5NeckInput gh5:
                gh5NeckInputType ??= gh5.Input;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, gh5.Sda, gh5.Scl);
                break;
            case InputType.WtNeckInput when Input.InnermostInput() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                if (this is GuitarAxis) ghWtInputType = GhWtInputType.TapBar;

                input = new GhWtTapInput(ghWtInputType.Value, Model, -1, -1, -1, -1);
                break;
            case InputType.WtNeckInput when Input.InnermostInput() is GhWtTapInput wt:
                ghWtInputType ??= wt.Input;
                input = new GhWtTapInput(ghWtInputType.Value, Model, wt.Pin, wt.PinS0, wt.PinS1, wt.PinS2);
                break;
            case InputType.WiiInput when Input.InnermostInput() is not WiiInput:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model);
                break;
            case InputType.WiiInput when Input.InnermostInput() is WiiInput wii:
                wiiInput ??= wii.Input;
                input = new WiiInput(wiiInput.Value, Model, wii.Sda, wii.Scl);
                break;
            case InputType.Ps2Input when Input.InnermostInput() is not Ps2Input:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model);
                break;
            case InputType.Ps2Input when Input.InnermostInput() is Ps2Input ps2:
                ps2InputType ??= ps2.Input;
                input = new Ps2Input(ps2InputType.Value, Model, ps2.Miso, ps2.Mosi, ps2.Sck,
                    ps2.Att,
                    ps2.Ack);
                break;
            default:
                return;
        }

        switch (input.IsAnalog)
        {
            case true when this is OutputAxis:
            case false when this is OutputButton:
                Input = input;
                break;
            case true when this is OutputButton:
                var oldType = input.IsUint ? AnalogToDigitalType.Trigger : AnalogToDigitalType.JoyHigh;
                var oldThreshold = input.IsUint ? ushort.MaxValue / 2 : short.MaxValue / 2;
                if (Input is AnalogToDigital atd)
                {
                    oldThreshold = atd.Threshold;
                    oldType = atd.AnalogToDigitalType;
                }

                Input = new AnalogToDigital(input, oldType, oldThreshold, Model);
                break;
            case false when this is GuitarAxis {Type: GuitarAxisType.Tilt}:
                Input = new DigitalToAnalog(input, Model);
                break;
            case false when this is OutputAxis axis:
                var oldOn = 0;
                if (Input is DigitalToAnalog dta)
                {
                    oldOn = dta.On;
                }
                Input = new DigitalToAnalog(input, oldOn, axis.Trigger, Model);
                break;
        }

        if (this is EmulationMode) Input = input;


        if (input.InnermostInput() is not DirectInput && this is OutputAxis axis2)
        {
            // Reset min and max to be safe
            if (Input.IsUint)
            {
                axis2.Min = ushort.MinValue;
                axis2.Max = ushort.MaxValue;
            }
            else
            {
                axis2.Min = short.MinValue;
                axis2.Max = short.MaxValue;
            }
        }

        this.RaisePropertyChanged(nameof(WiiInputType));
        this.RaisePropertyChanged(nameof(Ps2InputType));
        this.RaisePropertyChanged(nameof(GhWtInputType));
        this.RaisePropertyChanged(nameof(Gh5NeckInputType));
        this.RaisePropertyChanged(nameof(DjInputType));
    }


    public abstract SerializedOutput Serialize();

    public abstract string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros);

    public virtual IEnumerable<Output> ValidOutputs()
    {
        var (extra, _) = ControllerEnumConverter.FilterValidOutputs(Model.DeviceType, Model.RhythmType, Outputs.Items);
        return Outputs.Items.Except(extra).Where(output => output.Enabled);
    }

    [RelayCommand]
    private void Remove()
    {
        Model.RemoveOutput(this);
    }

    protected virtual IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return Enumerable.Empty<PinConfig>();
    }

    protected virtual IEnumerable<DevicePin> GetOwnPins()
    {
        return Enumerable.Empty<DevicePin>();
    }

    public List<PinConfig> GetPinConfigs()
    {
        return Outputs.Items
            .SelectMany(s => s.Outputs.Items).SelectMany(s => s.Input.Inputs()).SelectMany(s => s.PinConfigs)
            .Concat(GetOwnPinConfigs()).Distinct().ToList();
    }

    public virtual void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType, byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw, byte[] usbHostInputsRaw)
    {
        if (Enabled)
            Input.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                ghWtRaw,
                ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw);

        foreach (var output in Outputs.Items)
            if (output != this)
                output.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                    ghWtRaw,
                    ps2ControllerType, wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
    }

    public void UpdateErrors()
    {
        this.RaisePropertyChanged(nameof(ErrorText));
    }

    public abstract void UpdateBindings();
}