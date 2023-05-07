using System;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class InitialConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public InitialConfigViewModel(MainWindowViewModel screen, ConfigViewModel model)
    {
        Main = screen;
        Model = model;

        HostScreen = screen;

        ConfigureCommand = ReactiveCommand.CreateFromObservable(
            () => Main.Router.Navigate.Execute(model), this.WhenAnyValue(x => x.Main.Working).Select(s => !s)
        );
    }

    public MainWindowViewModel Main { get; }
    public ConfigViewModel Model { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> ConfigureCommand { get; }

    public string? UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            Model.AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            Model.RemoveDevice(change.Item.Current);
                            break;
                    }
            });
        ;
    }
}