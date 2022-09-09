using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Utils;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
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


        public ReactiveCommand<Unit, Unit> WriteConfig { get; }

        public ReactiveCommand<Unit, Unit> GoBack { get; }

        public ConfigViewModel(MainWindowViewModel screen)
        {
            Main = screen;
            HostScreen = screen;
            _config = this.WhenAnyValue(x => x.Main.SelectedDevice)
                .Where(x => x != null && x.GetType().IsAssignableTo(typeof(ConfigurableUsbDevice)))
                .Select(x => (x as ConfigurableUsbDevice)!.Configuration)
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
            WriteConfig = ReactiveCommand.CreateFromTask(Write, this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected)).ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
            GoBack = ReactiveCommand.CreateFromObservable<Unit, Unit>(Main.GoBack.Execute, this.WhenAnyValue(x => x.Main.Working).CombineLatest(this.WhenAnyValue(x => x.Main.Connected)).ObserveOn(RxApp.MainThreadScheduler).Select(x => !x.First && x.Second));
        }

        async Task Write()
        {
            await Main.Write(Config);
        }
    }
}