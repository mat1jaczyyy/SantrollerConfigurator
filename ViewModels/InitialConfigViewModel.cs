using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ProtoBuf;
using ReactiveUI;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class InitialConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public MainWindowViewModel Main { get; }
    public ConfigViewModel Model { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> ConfigureCommand { get; }
    public InitialConfigViewModel(MainWindowViewModel screen, ConfigViewModel model)
    {
        Main = screen;
        Model = model;

        HostScreen = screen;
        
        ConfigureCommand = ReactiveCommand.CreateFromObservable(
            () => Main.Router.Navigate.Execute(model), this.WhenAnyValue(x=>x.Main.Working).Select(s => !s)
        );
    }
    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().Subscribe(s =>
            {
                foreach (var change in s)
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            Model.AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            Model.RemoveDevice(change.Item.Current);
                            break;
                    }
                }
            });
        ;
    }

    public string? UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }
}