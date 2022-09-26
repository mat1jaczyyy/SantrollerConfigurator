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
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Utils;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {
        public Interaction<Output, AddInputWindowViewModel?> ShowDialog { get; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }

        public IEnumerable<DeviceControllerType> DeviceControllerTypes =>
            Enum.GetValues(typeof(DeviceControllerType)).Cast<DeviceControllerType>();

        public IEnumerable<RhythmType> RhythmTypes =>
            Enum.GetValues(typeof(RhythmType)).Cast<RhythmType>();

        public IEnumerable<EmulationType> EmulationTypes =>
            Enum.GetValues(typeof(EmulationType)).Cast<EmulationType>();

        public bool IsAVR => Main.SelectedDevice?.IsAVR() == true;
        public ReactiveCommand<Unit, Unit> WriteConfig { get; }

        public ReactiveCommand<Unit, Unit> GoBack { get; }

        public ReactiveCommand<Unit, Unit> Reset { get; }

        private LedType _ledType;

        public LedType LedType
        {
            get => _ledType;
            set => this.RaiseAndSetIfChanged(ref _ledType, value);
        }

        private bool _tiltEnabled;

        public bool TiltEnabled
        {
            get => _tiltEnabled;
            set => this.RaiseAndSetIfChanged(ref _tiltEnabled, value);
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
                this.RaisePropertyChanged("IsRhythm");
            }
        }

        private EmulationType _emulationType;

        public EmulationType EmulationType
        {
            get => _emulationType;
            set
            {
                this.SetDefaultBindings();
                this.RaiseAndSetIfChanged(ref _emulationType, value);
            }
        }

        private RhythmType _rhythmType;

        public RhythmType RhythmType
        {
            get => _rhythmType;
            set => this.RaiseAndSetIfChanged(ref _rhythmType, value);
        }

        private Microcontroller _microController;

        public Microcontroller MicroController
        {
            get => _microController;
            set => this.RaiseAndSetIfChanged(ref _microController, value);
        }

        private int _wtPin;

        public int WtPin
        {
            get => _wtPin;
            set => this.RaiseAndSetIfChanged(ref _wtPin, value);
        }


        public AvaloniaList<Output> Bindings { get; }
        public bool IsRhythm => _deviceControllerType is DeviceControllerType.Drum or DeviceControllerType.Guitar;

        public ConfigViewModel(MainWindowViewModel screen)
        {
            ShowDialog = new Interaction<Output, AddInputWindowViewModel?>();
            Main = screen;
            HostScreen = screen;

            WriteConfig = ReactiveCommand.CreateFromTask(Write,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, Unit>(Main.GoBack.Execute,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            Bindings = new AvaloniaList<Output>();
            ShowDialogCommand = ReactiveCommand.CreateFromObservable<Output, AddInputWindowViewModel?>((output) => ShowDialog.Handle(output));
        }

        public ICommand ShowDialogCommand { get; }

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
            EmulationType = EmulationType.Universal;
            RhythmType = RhythmType.GuitarHero;
            TiltEnabled = false;
            XInputOnWindows = false;
            SetDefaultBindings();
        }

        public void SetDefaultBindings()
        {
            Bindings.Clear();
            if (EmulationType == EmulationType.Universal)
            {
                //TODO: this should also add a IOutput for every controller output as well!
            }
        }


        public void Generate(PlatformIo pio)
        {
            var hasSpi = Bindings.Any(binding => binding.Input?.RequiresSpi() == true) || LedType == LedType.APA102;
            var hasI2C = Bindings.Any(binding => binding.Input?.RequiresI2C() == true);
            var inputs = Bindings.Select(binding => binding.Input?.InnermostInput()).OfType<IInput>().ToList();
            var directInputs = inputs.OfType<DirectInput>().ToList();
            string configFile = Path.Combine(pio.ProjectDir, "include", "config.h");
            var lines = File.ReadAllLines(configFile);
            var json = JsonSerializer.Serialize(this, JsonConfiguration.GetJsonOptions(MicroController));
            var bytes = Encoding.UTF8.GetBytes(json);
            var bytesLine = "";
            var bytesLenLine = "";
            using (var outputStream = new MemoryStream())
            {
                using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
                {
                    compressStream.Write(bytes, 0, bytes.Length);
                }

                bytesLine =
                    $"#define CONFIGURATION {{{string.Join(",", outputStream.ToArray().Select(b => "0x" + b.ToString("X")))}}}";
                bytesLenLine = $"#define CONFIGURATION_LEN {outputStream.ToArray().Length}";
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("#define WINDOWS_USES_XINPUT "))
                {
                    lines[i] = $"#define WINDOWS_USES_XINPUT {XInputOnWindows.ToString().ToLower()}";
                }

                if (line.StartsWith("#define TICK_PS3 "))
                {
                    lines[i] = $"#define TICK_PS3 {GenerateTick(false)}";
                }

                if (line.StartsWith("#define TICK_XINPUT "))
                {
                    lines[i] = $"#define TICK_XINPUT {GenerateTick(true)}";
                }

                if (line.StartsWith("#define ADC_COUNT "))
                {
                    lines[i] = $"#define ADC_COUNT {directInputs.Count(input => input.IsAnalog)}";
                }

                if (line.StartsWith("#define DIGITAL_COUNT "))
                {
                    lines[i] = $"#define DIGITAL_COUNT {inputs.Count(input => !input.IsAnalog)}";
                }

                if (line.StartsWith("#define LED_TYPE "))
                {
                    lines[i] = $"#define LED_TYPE {((byte) LedType)}";
                }

                if (line.StartsWith("#define TILT_ENABLED "))
                {
                    lines[i] = $"#define TILT_ENABLED {TiltEnabled.ToString().ToLower()}";
                }

                if (line.StartsWith("#define CONSOLE_TYPE "))
                {
                    lines[i] = $"#define CONSOLE_TYPE {((byte) EmulationType)}";
                }

                if (line.StartsWith("#define DEVICE_TYPE "))
                {
                    lines[i] = $"#define DEVICE_TYPE {((byte) DeviceType)}";
                }

                if (line.StartsWith("#define ADC_PINS "))
                {
                    // Sort by pin index, and then map to adc number and turn into an array
                    lines[i] =
                        $"#define ADC_PINS {{{String.Join(",", directInputs.OrderBy(s => s.Pin).Select(s => MicroController.GetChannel(s.Pin).ToString()))}}}";
                }

                if (line.StartsWith("#define PIN_INIT "))
                {
                    lines[i] = $"#define PIN_INIT {MicroController.GenerateInit(Bindings.ToList())}";
                }

                if (MicroController is Pico)
                {
                    if (line.StartsWith("#define SKIP_MASK_PICO "))
                    {
                        lines[i] = $"#define SKIP_MASK_PICO {MicroController.GenerateSkip(hasSpi, hasI2C)}";
                    }
                }
                else
                {
                    if (line.StartsWith("#define SKIP_MASK_AVR "))
                    {
                        lines[i] = $"#define SKIP_MASK_AVR {MicroController.GenerateSkip(hasSpi, hasI2C)}";
                    }
                }

                if (line.StartsWith("#define CONFIGURATION_LEN "))
                {
                    lines[i] = bytesLenLine;
                }
                else if (line.StartsWith("#define CONFIGURATION "))
                {
                    lines[i] = bytesLine;
                }
                else if (line.StartsWith("#define ARDWIINO_BOARD "))
                {
                    lines[i] = $"#define ARDWIINO_BOARD \"{MicroController.Board.ArdwiinoName}\"";
                }
            }

            File.WriteAllLines(configFile, lines);
        }

        public string GenerateTick(bool xbox)
        {
            var inputs = Bindings.Select(binding => binding.Input?.InnermostInput()).OfType<IInput>().ToList();
            var groupedInputs = Bindings.GroupBy(s => s.Input?.InnermostInput().GetType());
            string ret = "";
            int index = 0;
            bool combined = DeviceType == DeviceControllerType.Guitar && CombinedDebounce;
            if (combined)
            {
                index = 1;
            }

            foreach (var group in groupedInputs)
            {
                ret += group.First().Input?.InnermostInput().GenerateAll(xbox, group.Select(output =>
                {
                    var input = output.Input?.InnermostInput();
                    if (input != null)
                    {
                        var generated = output.Generate(xbox, MicroController);
                        if (output is OutputButton button)
                        {
                            if (combined && button.IsStrum())
                            {
                                generated = generated.Replace("debounce[i]", "debounce[0]");
                            }
                            else
                            {
                                generated = generated.Replace("debounce[i]", $"debounce[{index}]");
                                index++;
                            }
                        }

                        return new Tuple<IInput, string>(input, generated);
                    }

                    throw new IncompleteConfigurationException("Output without Input found!");
                }).ToList(), MicroController);
            }

            return ret;
        }
    }
}