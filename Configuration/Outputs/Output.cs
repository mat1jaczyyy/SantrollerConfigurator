using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Neck;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class LedIndex: ReactiveObject
{
    public Output Output { get; }
    public byte Index { get; }

    public LedIndex(Output output, byte i)
    {
        Output = output;
        Index = i;
    }

    public bool Selected
    {
        get => Output.LedIndices.Contains(Index);
        set
        {
            if (value)
            {
                Output.LedIndices.Add(Index);
            }
            else
            {
                Output.LedIndices.Remove(Index);
            }
            this.RaisePropertyChanged();
        }
    }
}

public abstract class Output : ReactiveObject, IDisposable
{
    protected readonly ConfigViewModel Model;

    private Input? _input;

    public Input? Input
    {
        get => _input;
        set => this.RaiseAndSetIfChanged(ref _input, value);
    }

    private readonly ObservableAsPropertyHelper<Bitmap?> _image;
    public Bitmap? Image => _image.Value;

    public string Name { get; }

    public InputType? SelectedInputType
    {
        get => Input?.InputType;
        set => SetInput(value, null, null, null, null, null);
    }

    public WiiInputType WiiInputType
    {
        get => (Input?.InnermostInput() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType, value, null, null, null, null);
    }

    public Ps2InputType Ps2InputType
    {
        get => (Input?.InnermostInput() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType, null, value, null, null, null);
    }

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => (Input?.InnermostInput() as Gh5NeckInput)?.Input ?? Gh5NeckInputType.Green;
        set => SetInput(SelectedInputType, null, null, null, value, null);
    }

    public DjInputType DjInputType
    {
        get => (Input?.InnermostInput() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType, null, null, null, null, value);
    }

    public GhWtInputType GhWtInputType
    {
        get => (Input?.InnermostInput() as GhWtTapInput)?.Input ?? GhWtInputType.TapGreen;
        set => SetInput(SelectedInputType, null, null, value, null, null);
    }

    public IEnumerable<GhWtInputType> GhWtInputTypes => Enum.GetValues<GhWtInputType>();

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes => Enum.GetValues<Gh5NeckInputType>();

    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes => Enum.GetValues<WiiInputType>();

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();

    public IEnumerable<InputType> InputTypes =>
        Enum.GetValues<InputType>().Where(s => s is not InputType.MacroInput || this is OutputButton);

    private readonly ObservableAsPropertyHelper<bool> _isDj;
    private readonly ObservableAsPropertyHelper<bool> _isWii;
    private readonly ObservableAsPropertyHelper<bool> _isPs2;
    private readonly ObservableAsPropertyHelper<bool> _isGh5;
    private readonly ObservableAsPropertyHelper<bool> _isWt;
    private readonly ObservableAsPropertyHelper<bool> _areLedsEnabled;
    private readonly ObservableAsPropertyHelper<string> _localisedName;

    public string LocalisedName => _localisedName.Value;
    public bool IsDj => _isDj.Value;
    public bool IsWii => _isWii.Value;
    public bool IsPs2 => _isPs2.Value;
    public bool IsGh5 => _isGh5.Value;
    public bool IsWt => _isWt.Value;
    public bool AreLedsEnabled => _areLedsEnabled.Value;

    public abstract bool IsCombined { get; }

    private Color _ledOn;
    private Color _ledOff;

    private readonly ObservableAsPropertyHelper<string> _ledIndicesDisplay;
    public string LedIndicesDisplay => _ledIndicesDisplay.Value;

    private readonly ObservableAsPropertyHelper<LedIndex[]> _availableIndices;
    public LedIndex[] AvailableIndices => _availableIndices.Value;
    public ObservableCollection<byte> LedIndices { get; set; }

    private readonly Guid _id = new Guid();
    public string Id => _id.ToString();

    private byte _ledIndex;

    public byte LedIndex
    {
        get => _ledIndex;
        set => this.RaiseAndSetIfChanged(ref _ledIndex, value);
    }

    public Color LedOn
    {
        get => _ledOn;
        set => this.RaiseAndSetIfChanged(ref _ledOn, value);
    }

    public Color LedOff
    {
        get => _ledOff;
        set => this.RaiseAndSetIfChanged(ref _ledOff, value);
    }

    private readonly ObservableAsPropertyHelper<double> _imageOpacity;

    public double ImageOpacity => _imageOpacity.Value;

    private readonly ObservableAsPropertyHelper<int> _valueRaw;
    public int ValueRaw => _valueRaw.Value;

    public ICommand AssignByKeyOrAxis { get; }

    protected Output(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, string name)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        LedIndices = new ObservableCollection<byte>(ledIndices);
        Name = name;
        Model = model;
        _availableIndices = this.WhenAnyValue(x => x.Model.LedCount)
            .Select(x => Enumerable.Range(1, x).Select(s => new LedIndex(this, (byte)s)).ToArray())
            .ToProperty(this, x => x.AvailableIndices);
        _image = this.WhenAnyValue(x => x.Model.DeviceType).Select(GetImage).ToProperty(this, x => x.Image);
        _isDj = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is DjInput)
            .ToProperty(this, x => x.IsDj);
        _isWii = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is WiiInput)
            .ToProperty(this, x => x.IsWii);
        _isGh5 = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is Gh5NeckInput)
            .ToProperty(this, x => x.IsGh5);
        _isPs2 = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is Ps2Input)
            .ToProperty(this, x => x.IsPs2);
        _isWt = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is GhWtTapInput)
            .ToProperty(this, x => x.IsWt);
        _areLedsEnabled = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is not LedType.None)
            .ToProperty(this, x => x.AreLedsEnabled);
        _localisedName = this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.RhythmType)
            .Select(x => GetName(x.Item1, x.Item2))
            .ToProperty(this, x => x.LocalisedName);
        _valueRaw = this.WhenAnyValue(x => x.Input!.RawValue).ToProperty(this, x => x.ValueRaw);
        _imageOpacity = this.WhenAnyValue(x => x.ValueRaw, x => x.Input, x => x.IsCombined)
            .Select(s => (s.Item3 || s.Item2?.IsAnalog == true) ? 1 : ((s.Item1 == 0 ? 0 : 0.35) + 0.65))
            .ToProperty(this, s => s.ImageOpacity);
        _ledIndicesDisplay = this.WhenAnyValue(x => x.LedIndices)
            .Select(s => string.Join(", ", s))
            .ToProperty(this, s => s.LedIndicesDisplay);
        AssignByKeyOrAxis = ReactiveCommand.CreateFromTask(FindAndAssign);
        Outputs = new SourceList<Output>();
        Outputs.Add(this);
        AnalogOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        DigitalOutputs = new ReadOnlyObservableCollection<Output>(new ObservableCollection<Output>());
        _ledOnLabel = this.WhenAnyValue(x => x.Input!.IsAnalog)
            .Select(s => s ? "Highest LED Colour" : "Pressed LED Colour").ToProperty(this, x => x.LedOnLabel);
        _ledOffLabel = this.WhenAnyValue(x => x.Input!.IsAnalog)
            .Select(s => s ? "Lowest LED Colour" : "Released LED Colour").ToProperty(this, x => x.LedOffLabel);
    }

    void LedSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
    {
    }


    public abstract bool IsStrum { get; }

    public virtual string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return Name;
    }

    public static string GetReportField(object type)
    {
        var typeName = type.ToString()!;
        return $"report->{char.ToLower(typeName[0])}{typeName[1..]}";
    }

    public async Task FindAndAssign()
    {
        ButtonText = "Move the mouse or click / press any key to use that input";
        await InputManager.Instance!.Process.FirstAsync();
        await Task.Delay(200);
        var lastEvent = await InputManager.Instance.Process.FirstAsync();
        Console.WriteLine(lastEvent);
        byte debounce = 1;
        int min = short.MinValue;
        int max = short.MaxValue;
        int deadzone = 0;
        if (this is OutputAxis axis)
        {
            min = axis.Min;
            max = axis.Max;
            deadzone = axis.DeadZone;
        }

        if (this is OutputButton button)
        {
            debounce = button.Debounce;
        }

        if (lastEvent is RawKeyEventArgs keyEventArgs)
        {
            Model.Bindings.Add(new KeyboardButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), debounce,
                keyEventArgs.Key));
            Model.RemoveOutput(this);
        }

        if (lastEvent is RawPointerEventArgs pointerEventArgs)
        {
            switch (pointerEventArgs.Type)
            {
                case RawPointerEventType.LeftButtonDown:
                case RawPointerEventType.LeftButtonUp:
                    Model.Bindings.Add(new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), debounce,
                        MouseButtonType.Left));
                    Model.RemoveOutput(this);
                    break;
                case RawPointerEventType.RightButtonDown:
                case RawPointerEventType.RightButtonUp:
                    Model.Bindings.Add(new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), debounce,
                        MouseButtonType.Right));
                    Model.RemoveOutput(this);
                    break;
                case RawPointerEventType.MiddleButtonDown:
                case RawPointerEventType.MiddleButtonUp:
                    Model.Bindings.Add(new MouseButton(Model, Input, LedOn, LedOff, LedIndices.ToArray(), debounce,
                        MouseButtonType.Middle));
                    Model.RemoveOutput(this);
                    break;
                case RawPointerEventType.Move:
                    await Task.Delay(100);
                    var last = await InputManager.Instance.Process.Where(s => s is RawPointerEventArgs)
                        .Cast<RawPointerEventArgs>().FirstAsync();
                    var diff = last.Position - pointerEventArgs.Position;
                    if (Math.Abs(diff.X) > Math.Abs(diff.Y))
                    {
                        Model.Bindings.Add(new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(), min, max,
                            deadzone,
                            MouseAxisType.X));
                    }
                    else
                    {
                        Model.Bindings.Add(new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(), min, max,
                            deadzone,
                            MouseAxisType.Y));
                    }

                    Model.RemoveOutput(this);
                    break;
            }
        }

        if (lastEvent is RawMouseWheelEventArgs mouseWheelEventArgs)
        {
            if (Math.Abs(mouseWheelEventArgs.Delta.X) > Math.Abs(mouseWheelEventArgs.Delta.Y))
            {
                Model.Bindings.Add(new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(), min, max, deadzone,
                    MouseAxisType.ScrollX));
            }
            else
            {
                Model.Bindings.Add(new MouseAxis(Model, Input, LedOn, LedOff, LedIndices.ToArray(), min, max, deadzone,
                    MouseAxisType.ScrollY));
            }

            Model.RemoveOutput(this);
        }

        ButtonText = "Click to assign";
    }

    private void SetInput(InputType? inputType, WiiInputType? wiiInput, Ps2InputType? ps2InputType,
        GhWtInputType? ghWtInputType, Gh5NeckInputType? gh5NeckInputType, DjInputType? djInputType)
    {
        Input input;
        var lastPin = 0;
        var pinMode = DevicePinMode.PullUp;
        if (Input?.InnermostInput() is DirectInput direct)
        {
            lastPin = direct.Pin;
            if (!direct.IsAnalog)
            {
                pinMode = direct.PinMode;
            }
        }

        switch (inputType)
        {
            case InputType.AnalogPinInput:
                input = new DirectInput(lastPin, DevicePinMode.Analog, Model, Model.MicroController!);
                break;
            case InputType.MacroInput:
                input = new MacroInput(new DirectInput(lastPin, pinMode, Model, Model.MicroController!),
                    new DirectInput(lastPin, pinMode, Model, Model.MicroController!), Model);
                break;
            case InputType.DigitalPinInput:
                input = new DirectInput(lastPin, pinMode, Model, Model.MicroController!);
                break;
            case InputType.TurntableInput when Input?.InnermostInput() is not DjInput:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model, Model.MicroController!);
                break;
            case InputType.TurntableInput when Input?.InnermostInput() is DjInput dj:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model, Model.MicroController!, dj.Sda, dj.Scl);
                break;
            case InputType.Gh5NeckInput when Input?.InnermostInput() is not Gh5NeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, Model.MicroController!);
                break;
            case InputType.Gh5NeckInput when Input?.InnermostInput() is Gh5NeckInput gh5:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model, Model.MicroController!, gh5.Sda, gh5.Scl);
                break;
            case InputType.WtNeckInput when Input?.InnermostInput() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model, Model.MicroController!);
                break;
            case InputType.WtNeckInput when Input?.InnermostInput() is GhWtTapInput wt:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model, Model.MicroController!, wt.Pin);
                break;
            case InputType.WiiInput when Input?.InnermostInput() is not WiiInput:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model, Model.MicroController!);
                break;
            case InputType.WiiInput when Input?.InnermostInput() is WiiInput wii:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model, Model.MicroController!, wii.Sda, wii.Scl);
                break;
            case InputType.Ps2Input when Input?.InnermostInput() is not Ps2Input:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model, Model.MicroController!);
                break;
            case InputType.Ps2Input when Input?.InnermostInput() is Ps2Input ps2:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model, Model.MicroController!, ps2.Miso, ps2.Mosi, ps2.Sck,
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
                var oldThreshold = 0;
                if (Input is AnalogToDigital atd)
                {
                    oldThreshold = atd.Threshold;
                }

                Input = new AnalogToDigital(input, AnalogToDigitalType.JoyHigh, oldThreshold, Model);
                break;
            case false when this is OutputAxis:
                var oldOn = 0;
                var oldOff = 0;
                if (Input is DigitalToAnalog dta)
                {
                    oldOn = dta.On;
                    oldOff = dta.Off;
                }

                Input = new DigitalToAnalog(input, oldOn, oldOff, Model);
                break;
        }

        this.RaisePropertyChanged(nameof(WiiInputType));
        this.RaisePropertyChanged(nameof(Ps2InputType));
        this.RaisePropertyChanged(nameof(GhWtInputType));
        this.RaisePropertyChanged(nameof(Gh5NeckInputType));
        this.RaisePropertyChanged(nameof(DjInputType));
    }


    public abstract SerializedOutput Serialize();

    public Bitmap? GetImage(DeviceControllerType type)
    {
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        string? bitmap = null;
        if (this is EmptyOutput)
        {
            bitmap = "Generic.png";
        }
        else if (IsCombined)
        {
            bitmap = $"Combined/{Name}.png";
        }
        else
        {
            bitmap = type switch
            {
                DeviceControllerType.Guitar => $"GH/{Name}.png",
                DeviceControllerType.Gamepad => $"Others/Xbox360/360_{Name}.png",
                _ => bitmap
            };
        }

        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        try
        {
            return new Bitmap(assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{bitmap}")));
        }
        catch (FileNotFoundException)
        {
            try
            {
                return new Bitmap(
                    assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/Others/Xbox360/360_{Name}.png")));
            }
            catch (FileNotFoundException)
            {
                return new Bitmap(assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/None.png")));
            }
        }
    }

    public abstract string Generate(DeviceEmulationMode mode,  List<int> debounceIndex, bool combined, string extra);

    public SourceList<Output> Outputs { get; }

    public ReadOnlyObservableCollection<Output> AnalogOutputs { get; set; }
    public ReadOnlyObservableCollection<Output> DigitalOutputs { get; set; }

    public void Remove()
    {
        Model.RemoveOutput(this);
    }

    public virtual void Dispose()
    {
        Input?.Dispose();
    }

    public abstract bool IsKeyboard { get; }
    public abstract bool IsController { get; }

    public bool ChildOfCombined => Model.IsCombinedChild(this);
    public abstract bool IsMidi { get; }
    public bool IsEmpty => this is EmptyOutput;

    public abstract bool Valid { get; }

    private string _buttonText = "Click to assign";

    public string ButtonText
    {
        get => _buttonText;
        set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }

    public List<PinConfig> GetPinConfigs() => Outputs.Items
        .SelectMany(s => s.Outputs.Items).SelectMany(s => (s.Input?.PinConfigs ?? Array.Empty<PinConfig>()))
        .Distinct().ToList();

    public List<DevicePin> GetPins() => Outputs.Items
        .SelectMany(s => s.Outputs.Items).SelectMany(s => (s.Input?.Pins ?? Array.Empty<DevicePin>()))
        .Distinct().ToList();

    public virtual void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType)
    {
        foreach (var output in Outputs.Items)
        {
            output.Input?.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                ghWtRaw,
                ps2ControllerType, wiiControllerType);
        }
    }

    public virtual string ErrorText
    {
        get
        {
            var text = string.Join(", ",
                GetPinConfigs().Select(s => s.ErrorText).Distinct().Where(s => !string.IsNullOrEmpty(s)));
            return string.IsNullOrEmpty(text) ? "" : $"* Error: Conflicting pins: {text}!";
        }
    }

    private ObservableAsPropertyHelper<string> _ledOnLabel;

    public string LedOnLabel => _ledOnLabel.Value;
    private ObservableAsPropertyHelper<string> _ledOffLabel;

    public string LedOffLabel => _ledOffLabel.Value;

    public void UpdateErrors()
    {
        this.RaisePropertyChanged(nameof(ErrorText));
    }

    public abstract void UpdateBindings();
}