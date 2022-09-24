using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dahomey.Json;
using Dahomey.Json.Serialization.Conventions;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Output;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
using GuitarConfiguratorSharp.NetCore.Utils;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }
    
        public IEnumerable<DeviceControllerType> DeviceControllerTypes => Enum.GetValues(typeof(DeviceControllerType)).Cast<DeviceControllerType>();
        public ReactiveCommand<Unit, Unit> WriteConfig { get; }

        public ReactiveCommand<Unit, Unit> GoBack { get; }

        public ReactiveCommand<Unit, Unit> Reset { get; }
        
        public LedType LedType { get; set; }
        public bool TiltEnabled { get; set; }
        public bool XInputOnWindows { get; set; }
        
        public bool CombinedDebounce { get; set; }
        public DeviceControllerType DeviceType { get; set; }
        public EmulationType EmulationType { get; set; }
        public RhythmType RhythmType { get; set; }
        public Configuration.Microcontroller.Microcontroller MicroController { get; set; }
        public int WtPin { get; set; }


        public List<IOutput> Bindings { get; }

        public ConfigViewModel(MainWindowViewModel screen)
        {
            Main = screen;
            HostScreen = screen;

            WriteConfig = ReactiveCommand.CreateFromTask(Write,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, Unit>(Main.GoBack.Execute,
                this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected))
                    .ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
        }

        async Task Write()
        {
            await Main.Write(this);
        }
        //TODO: JSON serialisation for ConfigViewModel
        // [JsonConstructor]
        // public DeviceConfiguration(Configuration.Microcontroller.Microcontroller microcontroller,
        //     List<IOutput> bindings, LedType ledType, DeviceControllerType deviceType, EmulationType emulationType,
        //     RhythmType rhythmType, bool tiltEnabled, bool xInputOnWindows)
        // {
        //     this.Bindings = bindings;
        //     this.MicroController = microcontroller;
        //     this.LedType = ledType;
        //     this.DeviceType = deviceType;
        //     this.EmulationType = emulationType;
        //     this.RhythmType = rhythmType;
        //     this.TiltEnabled = tiltEnabled;
        //     this.XInputOnWindows = xInputOnWindows;
        // }
        //
        public void SetDefaults(Configuration.Microcontroller.Microcontroller microcontroller)
        {
            MicroController = microcontroller;
            this.Bindings.Clear();
            this.LedType = LedType.None;
            this.DeviceType = DeviceControllerType.Gamepad;
            this.EmulationType = EmulationType.Universal;
            this.RhythmType = RhythmType.GuitarHero;
            this.TiltEnabled = false;
            this.XInputOnWindows = false;
        }

        public static JsonSerializerOptions GetJsonOptions(Configuration.Microcontroller.Microcontroller? controller)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new MicrocontrollerJsonConverter(controller));
            options.SetupExtensions();
            DiscriminatorConventionRegistry registry = options.GetDiscriminatorConventionRegistry();
            registry.RegisterType<DjInput>();
            registry.RegisterType<Gh5NeckInput>();
            registry.RegisterType<GhWtTapInput>();
            registry.RegisterType<AnalogToDigital>();
            registry.RegisterType<DigitalToAnalog>();
            registry.RegisterType<ControllerAxis>();
            registry.RegisterType<ControllerButton>();
            registry.RegisterType<KeyboardButton>();
            registry.RegisterType<Ps2Input>();
            registry.RegisterType<WiiInput>();
            registry.RegisterType<DirectInput>();
            return options;
        }

        public void Generate(PlatformIo pio)
        {
            var hasSpi = Bindings.Any(binding => binding.Input?.RequiresSpi() == true) || LedType == LedType.APA102;
            var hasI2C = Bindings.Any(binding => binding.Input?.RequiresI2C() == true);
            var inputs = Bindings.Select(binding => binding.Input?.InnermostInput()).OfType<IInput>().ToList();
            var directInputs = inputs.OfType<DirectInput>().ToList();
            string configFile = Path.Combine(pio.ProjectDir, "include", "config.h");
            var lines = File.ReadAllLines(configFile);
            var json = JsonSerializer.Serialize(this, GetJsonOptions(MicroController));
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
                    lines[i] = $"#define ADC_COUNT {directInputs.Count(input => input.IsAnalog())}";
                }

                if (line.StartsWith("#define DIGITAL_COUNT "))
                {
                    lines[i] = $"#define DIGITAL_COUNT {inputs.Count(input => !input.IsAnalog())}";
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
                    lines[i] = $"#define PIN_INIT {MicroController.GenerateInit(Bindings)}";
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

        public class MicrocontrollerJsonConverter : JsonConverter<Microcontroller>
        {
            private readonly Configuration.Microcontroller.Microcontroller? _currentController;

            public MicrocontrollerJsonConverter(Configuration.Microcontroller.Microcontroller? currentController)
            {
                _currentController = currentController;
            }

            public override Configuration.Microcontroller.Microcontroller Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options) => _currentController!;

            public override void Write(
                Utf8JsonWriter writer,
                Configuration.Microcontroller.Microcontroller dateTimeValue,
                JsonSerializerOptions options) => writer.WriteNullValue();
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
                }).ToList(),MicroController);
            }

            return ret;
        }
    }
}