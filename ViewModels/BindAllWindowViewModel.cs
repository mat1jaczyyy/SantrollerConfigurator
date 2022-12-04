using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels;

public class BindAllWindowViewModel : ReactiveObject
{
    public ConfigViewModel Model { get; }
    public Output Output { get; }
    public DirectInput Input { get; }

    public Microcontroller Microcontroller { get; }
    public ICommand YesCommand { get; }
    public ICommand NoCommand { get; }
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();
    public bool Response { get; set; }
    public bool IsAnalog { get; }
    public string LocalisedName { get; }

    public Bitmap? Image { get; }

    private volatile bool _picking = true;

    public BindAllWindowViewModel(ConfigViewModel model, Microcontroller microcontroller, Output output,
        DirectInput input)
    {
        Microcontroller = microcontroller;
        Model = model;
        Output = output;
        Input = input;
        IsAnalog = input.IsAnalog;
        Image = output.GetImage(model.DeviceType);
        LocalisedName = output.GetName(model.DeviceType, model.RhythmType);

        if (Model.Main.SelectedDevice is not Santroller santroller)
        {
            CloseWindowInteraction.Handle(new Unit());
            return;
        }
        YesCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            _picking = false;
            santroller.cancelDetection();
            Response = true;
            return CloseWindowInteraction.Handle(new Unit());
        });
        NoCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            _picking = false;
            santroller.cancelDetection();
            Response = false;
            return CloseWindowInteraction.Handle(new Unit());
        });
        Task.Run(async () =>
        {
            while (_picking)
            {
                try
                {
                    input.Pin = await santroller.DetectPin(IsAnalog, input.Pin, Microcontroller);
                    await Task.Delay(100);
                    Console.WriteLine(_picking);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        });
    }
}