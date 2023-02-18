using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using GuitarConfigurator.NetCore.Configuration;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class BindAllWindowViewModel : ReactiveObject
{
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();

    private volatile bool _picking = true;

    private readonly Santroller? _santroller;

    public BindAllWindowViewModel(ConfigViewModel model, Microcontroller microcontroller, Output output,
        DirectInput input)
    {
        Microcontroller = microcontroller;
        Model = model;
        Output = output;
        Input = input;
        IsAnalog = input.IsAnalog;
        Image = output.GetImage(model.DeviceType, model.RhythmType);
        LocalisedName = output.GetName(model.DeviceType, model.RhythmType);

        ContinueCommand = ReactiveCommand.CreateFromObservable(() => Close(true));
        AbortCommand = ReactiveCommand.CreateFromObservable(() => Close(false));

        if (Model.Main.SelectedDevice is not Santroller santroller)
        {
            CloseWindowInteraction.Handle(new Unit());
            _santroller = null;
            return;
        }

        _santroller = santroller;
        Task.Run(async () =>
        {
            while (_picking)
            {
                input.Pin = await santroller.DetectPin(IsAnalog, input.Pin, Microcontroller);
                await Task.Delay(100);
            }
        });
    }

    public ConfigViewModel Model { get; }
    public Output Output { get; }
    public DirectInput Input { get; }

    public Microcontroller Microcontroller { get; }
    public ICommand ContinueCommand { get; }
    public ICommand AbortCommand { get; }
    public bool Response { get; set; }
    public bool IsAnalog { get; }
    public string LocalisedName { get; }

    public Bitmap? Image { get; }

    private IObservable<Unit> Close(bool response)
    {
        _picking = false;
        _santroller?.cancelDetection();
        Response = response;
        return CloseWindowInteraction.Handle(new Unit());
    }
}