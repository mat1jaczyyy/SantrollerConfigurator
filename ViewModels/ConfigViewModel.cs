using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using ReactiveUI;
using System.ComponentModel;
using GuitarConfiguratorSharp.Configuration;
using System.Reactive.Linq;
using System.Collections.Generic;
using GuitarConfiguratorSharp.Utils;
using System.Reactive;

namespace GuitarConfiguratorSharp.ViewModels
{
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }

        private readonly ObservableAsPropertyHelper<IEnumerable<Button>> _bindings;
        public IEnumerable<Button> Bindings => _bindings.Value;
        private readonly ObservableAsPropertyHelper<IEnumerable<AnalogToDigital>> _analogToDigitalBindings;
        public IEnumerable<AnalogToDigital> AnalogToDigitalBindings => _analogToDigitalBindings.Value;
        private readonly ObservableAsPropertyHelper<IEnumerable<DigitalToAnalog>> _digitalToAnalogBindings;
        public IEnumerable<DigitalToAnalog> DigitalToAnalogBindings => _digitalToAnalogBindings.Value;
        private readonly ObservableAsPropertyHelper<IEnumerable<Axis>> _analogBindings;
        public IEnumerable<Axis> AnalogBindings => _analogBindings.Value;

        private readonly ObservableAsPropertyHelper<DeviceConfiguration> _config;
        public DeviceConfiguration Config => _config.Value;


        public ReactiveCommand<Unit, Unit> Write { get; }

        public ReactiveCommand<Unit, Unit> GoBack { get; }

        public ConfigViewModel(MainWindowViewModel screen)
        {
            Main = screen;
            HostScreen = screen;
            _config = this.WhenAnyValue(x => x.Main.SelectedDevice)
                .Where(x => x != null && x.GetType().IsAssignableTo(typeof(ConfigurableUSBDevice)))
                .Select(x => (x as ConfigurableUSBDevice)!.Configuration)
                .ToProperty(this, x => x.Config);

            _bindings = this.WhenAnyValue(x => x.Config)
                .Select(x => x.Bindings.FilterCast<Binding, Button>().Where(x => x as AnalogToDigital == null))
                .ToProperty(this, x => x.Bindings);

            _analogBindings = this.WhenAnyValue(x => x.Config)
                .Select(x => x.Bindings.FilterCast<Binding, Axis>().Where(x => x as DigitalToAnalog == null))
                .ToProperty(this, x => x.AnalogBindings);

            _digitalToAnalogBindings = this.WhenAnyValue(x => x.Config)
                .Select(x => x.Bindings.FilterCast<Binding, DigitalToAnalog>())
                .ToProperty(this, x => x.DigitalToAnalogBindings);

            _analogToDigitalBindings = this.WhenAnyValue(x => x.Config)
                .Select(x => x.Bindings.FilterCast<Binding, AnalogToDigital>())
                .ToProperty(this, x => x.AnalogToDigitalBindings);
            Write = ReactiveCommand.Create(write, this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected)).ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, Unit>(Main.GoBack.Execute, this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected)).ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
        }

        async void write()
        {
            Config.generate(Main.pio);
            if (Config.MicroController.Board.hasUSBMCU)
            {
                await Main.pio.RunPlatformIO(Config.MicroController.Board.environment+"-usb", "run --target upload", "Writing - USB", 0, 0, 40, Main.SelectedDevice);
                // await Main.pio.RunPlatformIO(Config.MicroController.Board.environment, "run --target upload", "Writing - Main", 0, 50, 90, Main.SelectedDevice);
            }
            else
            {
                await Main.pio.RunPlatformIO(Config.MicroController.Board.environment, "run --target upload", "Writing", 0, 0, 90, Main.SelectedDevice);
            }
        }
    }
}