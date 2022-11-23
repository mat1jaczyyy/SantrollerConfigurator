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
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Utils;
using ProtoBuf;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {
        public Interaction<InputWithPin, SelectPinWindowViewModel?> ShowPinSelectDialog { get; }
        public Interaction<Arduino, ShowUnoShortWindowViewModel?> ShowUnoShortDialog { get; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }

        public IEnumerable<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>();

        public IEnumerable<RhythmType> RhythmTypes => Enum.GetValues<RhythmType>();

        public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>();

        public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

        //TODO: actually read and write this as part of the config
        public bool KvEnabled { get; set; } = false;
        public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
        public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

        public ICommand WriteConfig { get; }

        public ICommand GoBack { get; }

        private LedOrderType _ledOrder;

        public LedOrderType LedOrder
        {
            get => _ledOrder;
            set => this.RaiseAndSetIfChanged(ref _ledOrder, value);
        }

        private LedType _ledType;

        public LedType LedType
        {
            get => _ledType;
            set => this.RaiseAndSetIfChanged(ref _ledType, value);
        }

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
            set
            {
                this.RaiseAndSetIfChanged(ref _deviceControllerType, value);
                UpdateBindings();
            }
        }

        private EmulationType _emulationType;

        public EmulationType EmulationType
        {
            get => _emulationType;
            set
            {
                this.RaiseAndSetIfChanged(ref _emulationType, value);
                SetDefaultBindings();
            }
        }

        private RhythmType _rhythmType;

        public RhythmType RhythmType
        {
            get => _rhythmType;
            set
            {
                this.RaiseAndSetIfChanged(ref _rhythmType, value);
                UpdateBindings();
            }
        }

        private void UpdateBindings()
        {
            Bindings.RemoveAll(Bindings.Where(binding => binding.LocalisedName == null).ToList());
            // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
            if (Bindings.Any(s => s is WiiCombinedOutput or Ps2CombinedOutput))
            {
                return;
            }

            var types = ControllerEnumConverter.GetTypes((_deviceControllerType, _rhythmType))
                .Where(s => s is not SimpleType).ToList();
            foreach (var binding in Bindings)
            {
                switch (binding)
                {
                    case ControllerButton button:
                        types.Remove(button.Type);
                        break;
                    case ControllerAxis axis:
                        types.Remove(axis.Type);
                        break;
                }
            }

            if (_deviceControllerType == DeviceControllerType.TurnTable)
            {
                if (!Bindings.Any(s => s is DjCombinedOutput))
                {
                    Bindings.Add(new DjCombinedOutput(this, MicroController!));
                }

                var combined = Bindings.OfType<DjCombinedOutput>().First();
                var outputs = combined.Outputs;
                foreach (var output in outputs)
                {
                    switch (output)
                    {
                        case ControllerButton button:
                            types.Remove(button.Type);
                            break;
                        case ControllerAxis axis:
                            types.Remove(axis.Type);
                            break;
                    }
                }
            }

            if (_deviceControllerType == DeviceControllerType.TurnTable && !Bindings.Any(s => s is DjCombinedOutput))
            {
                Bindings.Add(new DjCombinedOutput(this, MicroController!));
            }

            foreach (var type in types)
            {
                switch (type)
                {
                    case StandardButtonType buttonType:
                        Bindings.Add(new ControllerButton(this,
                            new DirectInput(0, DevicePinMode.PullUp, this, MicroController!),
                            Colors.Transparent, Colors.Transparent, null, 1, buttonType));
                        break;
                    case StandardAxisType axisType:
                        Bindings.Add(new ControllerAxis(this,
                            new DirectInput(0, DevicePinMode.Analog, this, MicroController!),
                            Colors.Transparent, Colors.Transparent, null, 1, 0, 0, axisType));
                        break;
                }
            }
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
            ShowUnoShortDialog = new Interaction<Arduino, ShowUnoShortWindowViewModel?>();
            Main = screen;
            HostScreen = screen;

            WriteConfig = ReactiveCommand.CreateFromTask(Write,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel?>(Main.GoBack.Execute,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            Bindings = new AvaloniaList<Output>();
            ShowPinSelectDialogCommand =
                ReactiveCommand.CreateFromObservable<InputWithPin, SelectPinWindowViewModel?>(output =>
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

        public async Task Write()
        {
            await Main.Write(this);
        }

        public async Task SetDefaults(Microcontroller microcontroller)
        {
            MicroController = microcontroller;
            LedType = LedType.None;
            DeviceType = DeviceControllerType.Gamepad;
            EmulationType = EmulationType.Controller;
            RhythmType = RhythmType.GuitarHero;
            XInputOnWindows = false;
            ClearOutputs();

            switch (Main.DeviceInputType)
            {
                case DeviceInputType.Direct:
                    SetDefaultBindings();
                    break;
                case DeviceInputType.Wii:
                    Bindings.Add(new WiiCombinedOutput(this, microcontroller));
                    break;
                case DeviceInputType.Ps2:
                    Bindings.Add(new Ps2CombinedOutput(this, microcontroller));
                    break;
            }

            if (Main.IsUno || Main.IsMega)
            {
                await Task.WhenAll(Write(), ShowUnoShortDialog.Handle((Arduino) Main.SelectedDevice!).ToTask());
                return;
            }

            await Write();
        }

        public void SetDefaultBindings()
        {
            ClearOutputs();
            if (EmulationType != EmulationType.Controller) return;
            foreach (var type in Enum.GetValues<StandardAxisType>())
            {
                if (ControllerEnumConverter.GetAxisText(_deviceControllerType, _rhythmType, type) == null) continue;
                Bindings.Add(new ControllerAxis(this,
                    new DirectInput(MicroController!.GetFirstAnalogPin(), DevicePinMode.Analog, this, MicroController!),
                    Colors.Transparent, Colors.Transparent, null, 1, 0, 0, type));
            }

            foreach (var type in Enum.GetValues<StandardButtonType>())
            {
                if (ControllerEnumConverter.GetButtonText(_deviceControllerType, _rhythmType, type) ==
                    null) continue;
                Bindings.Add(new ControllerButton(this, new DirectInput(0, DevicePinMode.PullUp, this, MicroController!),
                    Colors.Transparent, Colors.Transparent, null, 1, type));
            }
        }

        public void Generate(PlatformIo pio)
        {
            if (_microController == null) return;
            var outputs = Bindings.SelectMany(binding => binding.Outputs).ToList();
            var inputs = outputs.Select(binding => binding.Input?.InnermostInput()).OfType<Input>().ToList();
            var directInputs = inputs.OfType<DirectInput>().ToList();
            var configFile = Path.Combine(pio.ProjectDir, "include", "config_data.h");
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

            lines.Add($"#define TICK_SHARED {GenerateTick(false, true)}");

            lines.Add($"#define TICK_PS3 {GenerateTick(false, false)}");

            lines.Add($"#define TICK_XINPUT {GenerateTick(true, false)}");

            lines.Add(
                $"#define ADC_COUNT {directInputs.DistinctBy(s => s.PinConfig.Pin).Count(input => input.IsAnalog)}");

            lines.Add($"#define DIGITAL_COUNT {CalculateDebounceTicks()}");
            //TODO: this
            lines.Add($"#define LED_COUNT 0");

            lines.Add($"#define LED_TYPE {((byte) LedType)}");

            lines.Add($"#define CONSOLE_TYPE {((byte) EmulationType)}");

            lines.Add($"#define DEVICE_TYPE {((byte) DeviceType)}");

            lines.Add($"#define RHYTHM_TYPE {((byte) RhythmType)}");
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
                $"#define ADC_PINS {{{string.Join(",", directInputs.Where(s => s.IsAnalog).OrderBy(s => s.PinConfig.Pin).Select(s => _microController.GetChannel(s.PinConfig.Pin).ToString()).Distinct())}}}");

            lines.Add($"#define PIN_INIT {_microController.GenerateInit()}");

            lines.Add(_microController.GenerateDefinitions());

            lines.Add($"#define ARDWIINO_BOARD \"{_microController.Board.ArdwiinoName}\"");
            lines.Add(string.Join("\n",
                inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}")));

            File.WriteAllLines(configFile, lines);
        }

        public void RemoveOutput(Output output)
        {
            output.Dispose();
            if (Bindings.Remove(output)) return;
            foreach (var binding in Bindings)
            {
                binding.Outputs.Remove(output);
            }
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
            Bindings.Add(new EmptyOutput(this));
        }

        private string GenerateTick(bool xbox, bool shared)
        {
            if (_microController == null) return "";
            var outputs = Bindings.SelectMany(binding => binding.Outputs).ToList();
            var groupedOutputs = outputs.GroupBy(s => s.Input?.InnermostInput().GetType());
            var combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;

            Dictionary<string, int> debounces = new();
            HashSet<string> debounceTicks = new();
            Dictionary<int, string> leds = new();
            if (combined)
            {
                foreach (var output in outputs.Where(output => output.IsStrum))
                {
                    debounces[output.Name] = debounces.Count;
                }
            }

            var ret = groupedOutputs.Aggregate("", (current, group) => current + (group.First()
                .Input?.InnermostInput()
                .GenerateAll(group.Select(output =>
                    {
                        var input = output.Input?.InnermostInput();
                        if (input == null) throw new IncompleteConfigurationException("Output without Input found!");
                        var index = 0;
                        if (output is OutputButton button)
                        {
                            if (!debounces.ContainsKey(output.Name))
                            {
                                debounces[output.Name] = debounces.Count;
                            }

                            index = debounces[output.Name];
                            debounceTicks.Add(button.GenerateDebounceUpdate(index, xbox));
                        }

                        if (_ledType == LedType.Apa102 && output.LedIndex.HasValue)
                        {
                            leds[output.LedIndex.Value] = output.GenerateLedUpdate(index, xbox);
                        }

                        var generated = output.Generate(xbox, shared, index, combined);
                        return new Tuple<Input, string>(input, generated);
                    })
                    .Where(s => !string.IsNullOrEmpty(s.Item2))
                    .ToList()) + ";"));
            if (!shared)
            {
                //For any missing leds, we need to pad out the indexes so that the leds are aligned
                if (leds.Any())
                {
                    var max = leds.Keys.Max();
                    for (var i = 0; i < max; i++)
                    {
                        if (!leds.ContainsKey(i))
                        {
                            leds[i] =
                                @"spi_transfer(APA102_SPI_PORT, 0xff);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);";
                        }
                    }
                }

                var ticks = debounceTicks.ToList();
                ticks.InsertRange(0, leds.OrderBy(led => led.Key).Select(s => s.Value));
                ret = ticks.Aggregate(ret, (current, debounceTick) => current + debounceTick);
            }

            return ret.Replace('\n', ' ');
        }

        private int CalculateDebounceTicks()
        {
            var combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;
            var count = Bindings.SelectMany(binding => binding.Outputs)
                .Where(s => s is OutputButton button && (!combined || !button.IsStrum)).Select(s => s.Name).Distinct()
                .Count();
            if (combined)
            {
                count++;
            }

            return count;
        }

        public bool IsCombinedChild(Output output)
        {
            return Bindings.Contains(output);
        }
    }
}