using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Utils;
using ProtoBuf;
using ProtoBuf.Meta;
using ReactiveUI;
using MouseButton = GuitarConfiguratorSharp.NetCore.Configuration.Outputs.MouseButton;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    // ReSharper disable ExplicitCallerInfoArgument
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {
        public Interaction<InputWithPin, SelectPinWindowViewModel?> ShowPinSelectDialog { get; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }

        public IEnumerable<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>();

        public IEnumerable<RhythmType> RhythmTypes => Enum.GetValues<RhythmType>();

        public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>();

        public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

        public IEnumerable<SimpleType> SimpleTypes => Enum.GetValues<SimpleType>();

        //TODO: actually read and write this as part of the config
        public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
        public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

        public ICommand WriteConfig { get; }

        public ICommand GoBack { get; }

        private LedType _ledType;

        public LedType LedType
        {
            get => _ledType;
            set => this.RaiseAndSetIfChanged(ref _ledType, value);
        }

        private SimpleType? _simpleType;

        public SimpleType? SimpleType
        {
            get => _simpleType;
            set
            {
                this.RaiseAndSetIfChanged(ref _simpleType, value);
                this.RaiseAndSetIfChanged(ref _axisType, null, "StandardAxisType");
                this.RaiseAndSetIfChanged(ref _buttonType, null, "StandardButtonType");
            }
        }

        private StandardButtonType? _buttonType;

        public StandardButtonType? StandardButtonType
        {
            get => _buttonType;
            set
            {
                this.RaiseAndSetIfChanged(ref _buttonType, value);
                this.RaiseAndSetIfChanged(ref _simpleType, null, "SimpleType");
                this.RaiseAndSetIfChanged(ref _axisType, null, "StandardAxisType");
            }
        }

        public IEnumerable<StandardButtonType> StandardButtonTypes => Enum.GetValues<StandardButtonType>();

        private StandardAxisType? _axisType;

        public StandardAxisType? StandardAxisType
        {
            get => _axisType;
            set
            {
                this.RaiseAndSetIfChanged(ref _axisType, value);
                this.RaiseAndSetIfChanged(ref _simpleType, null, "SimpleType");
                this.RaiseAndSetIfChanged(ref _buttonType, null, "StandardButtonType");
            }
        }

        // TODO Somehow these will all need to be localised to the current controller type
        public IEnumerable<StandardAxisType> StandardAxisTypes => Enum.GetValues<StandardAxisType>();

        private Key? _key;

        public Key? Key
        {
            get => _key;
            set
            {
                this.RaiseAndSetIfChanged(ref _key, value);
                this.RaiseAndSetIfChanged(ref _mouseAxisType, null, "MouseAxisType");
                this.RaiseAndSetIfChanged(ref _mouseButtonType, null, "MouseButtonType");
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
                this.RaiseAndSetIfChanged(ref _mouseButtonType, null, "MouseButtonType");
                this.RaiseAndSetIfChanged(ref _key, null, "Key");
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
                this.RaiseAndSetIfChanged(ref _mouseAxisType, null, "MouseAxisType");
                this.RaiseAndSetIfChanged(ref _key, null, "Key");
            }
        }

        public IEnumerable<MouseButtonType> MouseButtonTypes => Enum.GetValues<MouseButtonType>();

        private bool _xinputOnWindows;

        public bool XInputOnWindows
        {
            get => _xinputOnWindows;
            set => this.RaiseAndSetIfChanged(ref _xinputOnWindows, value);
        }

        private bool _combinedDebounce;

        public bool CombinedDebounce
        {
            get => _combinedDebounce;
            set => this.RaiseAndSetIfChanged(ref _combinedDebounce, value);
        }

        private DeviceControllerType _deviceControllerType;

        public DeviceControllerType DeviceType
        {
            get => _deviceControllerType;
            set => this.RaiseAndSetIfChanged(ref _deviceControllerType, value);
        }

        private EmulationType _emulationType;

        public EmulationType EmulationType
        {
            get => _emulationType;
            set
            {
                this.RaiseAndSetIfChanged(ref _emulationType, value);
                this.SetDefaultBindings();
            }
        }

        private RhythmType _rhythmType;

        public RhythmType RhythmType
        {
            get => _rhythmType;
            set => this.RaiseAndSetIfChanged(ref _rhythmType, value);
        }

        private Microcontroller? _microController;

        public Microcontroller? MicroController
        {
            get => _microController;
            set => this.RaiseAndSetIfChanged(ref _microController, value);
        }


        public AvaloniaList<Output> Bindings { get; }
        private readonly ObservableAsPropertyHelper<bool> _isRhythm;
        public bool IsRhythm => _isRhythm.Value;
        private readonly ObservableAsPropertyHelper<bool> _isController;
        public bool IsController => _isController.Value;
        private readonly ObservableAsPropertyHelper<bool> _isKeyboard;
        public bool IsKeyboard => _isKeyboard.Value;
        private readonly ObservableAsPropertyHelper<bool> _isMidi;
        public bool IsMidi => _isMidi.Value;

        public ConfigViewModel(MainWindowViewModel screen)
        {
            ShowPinSelectDialog = new Interaction<InputWithPin, SelectPinWindowViewModel?>();
            Main = screen;
            HostScreen = screen;

            WriteConfig = ReactiveCommand.CreateFromTask(Write,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, Unit>(Main.GoBack.Execute,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            Bindings = new AvaloniaList<Output>();
            ShowPinSelectDialogCommand =
                ReactiveCommand.CreateFromObservable<InputWithPin, SelectPinWindowViewModel?>((output) =>
                    ShowPinSelectDialog.Handle(output));
            _isRhythm = this.WhenAnyValue(x => x.DeviceType)
                .Select(x => x is DeviceControllerType.Drum or DeviceControllerType.Guitar)
                .ToProperty(this, x => x.IsRhythm);
            _isController = this.WhenAnyValue(x => x.EmulationType)
                .Select(x => x is EmulationType.Controller)
                .ToProperty(this, x => x.IsController);
            _isKeyboard = this.WhenAnyValue(x => x.EmulationType)
                .Select(x => x is EmulationType.KeyboardMouse)
                .ToProperty(this, x => x.IsKeyboard);
            _isMidi = this.WhenAnyValue(x => x.EmulationType)
                .Select(x => x is EmulationType.Midi)
                .ToProperty(this, x => x.IsMidi);
        }

        public ICommand ShowPinSelectDialogCommand { get; }

        async Task Write()
        {
            Generate(Main.Pio);
            await Main.Write(this);
        }

        public void SetDefaults(Microcontroller microcontroller)
        {
            MicroController = microcontroller;
            LedType = LedType.None;
            DeviceType = DeviceControllerType.Gamepad;
            EmulationType = EmulationType.Controller;
            RhythmType = RhythmType.GuitarHero;
            XInputOnWindows = false;
            SetDefaultBindings();
        }

        public void SetDefaultBindings()
        {
            ClearOutputs();
            if (EmulationType == EmulationType.Controller)
            {
                foreach (var type in Enum.GetValues<StandardAxisType>())
                {
                    Bindings.Add(new ControllerAxis(this, new DirectInput(0, DevicePinMode.Analog, MicroController!),
                        Colors.Transparent, Colors.Transparent, 1, 0, 0, type));
                }

                foreach (var type in Enum.GetValues<StandardButtonType>())
                {
                    Bindings.Add(new ControllerButton(this, new DirectInput(0, DevicePinMode.PullUp, MicroController!),
                        Colors.Transparent, Colors.Transparent, 1, type));
                }
            }
        }

        public void Generate(PlatformIo pio)
        {
            if (_microController == null) return;
            var outputs = Bindings.SelectMany(binding => binding.Outputs).ToList();
            var inputs = outputs.Select(binding => binding.Input?.InnermostInput()).OfType<Input>().ToList();
            var directInputs = inputs.OfType<DirectInput>().ToList();
            string configFile = Path.Combine(pio.ProjectDir, "include", "config_data.h");
            var lines = new List<string>();
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

            lines.Add($"#define TICK_PS3 {GenerateTick(false)}");

            lines.Add($"#define TICK_XINPUT {GenerateTick(true)}");

            lines.Add($"#define ADC_COUNT {directInputs.Count(input => input.IsAnalog)}");

            lines.Add($"#define DIGITAL_COUNT {CalculateDebounceTicks()}");

            lines.Add($"#define LED_TYPE {((byte) LedType)}");

            lines.Add($"#define CONSOLE_TYPE {((byte) EmulationType)}");

            lines.Add($"#define DEVICE_TYPE {((byte) DeviceType)}");
            lines.Add($"#define KV_KEY_1 {{{string.Join(",", KvKey1.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
            lines.Add($"#define KV_KEY_2 {{{string.Join(",", KvKey2.ToArray().Select(b => "0x" + b.ToString("X")))}}}");

            lines.Add(Ps2Input.GeneratePs2Pressures(inputs));

            // Sort by pin index, and then map to adc number and turn into an array
            lines.Add(
                $"#define ADC_PINS {{{String.Join(",", directInputs.OrderBy(s => s.Pin).Select(s => _microController.GetChannel(s.Pin).ToString()))}}}");

            lines.Add($"#define PIN_INIT {_microController.GenerateInit(outputs)}");

            lines.Add(_microController.GenerateDefinitions());

            lines.Add($"#define ARDWIINO_BOARD \"{_microController.Board.ArdwiinoName}\"");
            lines.Add(String.Join("\n",
                inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}")));

            File.WriteAllLines(configFile, lines);
        }

        public void RemoveOutput(Output output)
        {
            output.Dispose();
            Bindings.Remove(output);
        }

        public void ClearOutputs()
        {
            foreach (var binding in Bindings)
            {
                binding.Dispose();
            }

            Bindings.Clear();
        }

        public void Reset()
        {
            SetDefaultBindings();
        }

        public void AddOutput()
        {
            if (_microController == null) return;
            switch (EmulationType)
            {
                case EmulationType.Controller:
                    if (SimpleType.HasValue)
                    {
                        switch (SimpleType)
                        {
                            case Configuration.Types.SimpleType.WiiInputSimple:
                                Bindings.Add(new WiiCombinedOutput(this, _microController));
                                break;
                            case Configuration.Types.SimpleType.Gh5NeckSimple:
                                Bindings.Add(new Gh5CombinedOutput(this, _microController));
                                break;
                            case Configuration.Types.SimpleType.Ps2InputSimple:
                                Bindings.Add(new Ps2CombinedOutput(this, _microController));
                                break;
                            case Configuration.Types.SimpleType.WtNeckSimple:
                                Bindings.Add(new GhwtCombinedOutput(this, _microController));
                                break;
                            case Configuration.Types.SimpleType.DjTurntableSimple:
                                Bindings.Add(new DjCombinedOutput(this, _microController));
                                break;
                        }
                    }
                    else if (StandardAxisType.HasValue)
                    {
                        Bindings.Add(new ControllerAxis(this, null, Colors.Transparent, Colors.Transparent, 1, 0, 0,
                            StandardAxisType.Value));
                    }
                    else if (StandardButtonType.HasValue)
                    {
                        Bindings.Add(new ControllerButton(this, null, Colors.Transparent, Colors.Transparent, 5,
                            StandardButtonType.Value));
                    }

                    break;
                case EmulationType.KeyboardMouse:
                    if (MouseAxisType.HasValue)
                    {
                        Bindings.Add(new MouseAxis(this, null, Colors.Transparent, Colors.Transparent, 1, 0, 0,
                            MouseAxisType.Value));
                    }
                    else if (MouseButtonType.HasValue)
                    {
                        Bindings.Add(new MouseButton(this, null, Colors.Transparent, Colors.Transparent, 5,
                            MouseButtonType.Value));
                    }
                    else if (Key.HasValue)
                    {
                        Bindings.Add(new KeyboardButton(this, null, Colors.Transparent, Colors.Transparent, 5,
                            Key.Value));
                    }

                    break;
            }
        }

        public string GenerateTick(bool xbox)
        {
            if (_microController == null) return "";
            var outputs = Bindings.SelectMany(binding => binding.Outputs).ToList();
            var groupedOutputs = outputs.GroupBy(s => s.Input?.InnermostInput().GetType());
            string ret = "";
            bool combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;

            Dictionary<string, int> debounces = new();
            HashSet<string> debounceTicks = new();
            int indexCount = 0;
            if (combined)
            {
                indexCount++;
            }

            foreach (var group in groupedOutputs)
            {
                ret += group.First().Input?.InnermostInput().GenerateAll(xbox, group.Select(output =>
                {
                    var input = output.Input?.InnermostInput();
                    if (input != null)
                    {
                        
                        var generated = output.Generate(xbox, 0);
                        if (output is OutputButton button)
                        {
                            if (combined && button.IsStrum)
                            {
                                debounceTicks.Add(button.GenerateDebounceUpdate(0, xbox));
                            }
                            else
                            {
                                if (!debounces.ContainsKey(output.Name))
                                {
                                    debounces[output.Name] = indexCount++;
                                }

                                var index = debounces[output.Name];
                                generated = output.Generate(xbox, index);
                                debounceTicks.Add(button.GenerateDebounceUpdate(index, xbox));
                            }
                        }

                        return new Tuple<Input, string>(input, generated);
                    }

                    throw new IncompleteConfigurationException("Output without Input found!");
                }).ToList(), _microController) + ";";
            }

            //TODO: for apa102, the easiest option would be to do something like this, but for each output linked to an LED.
            //TODO: that should be pretty straightofward, we could even just implement a LED function on each output / input if we wanted to handle that correctly.
            foreach (var debounceTick in debounceTicks)
            {
                ret += debounceTick;
            }

            return ret.Replace('\n', ' ');
        }

        public int CalculateDebounceTicks()
        {
            bool combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;
            var count = Bindings.SelectMany(binding => binding.Outputs).Where(s => s is OutputButton button && (!combined || !button.IsStrum)).Select(s => s.Name).Distinct().Count();
            if (combined)
            {
                count++;
            }

            return count;
        }
    }
}