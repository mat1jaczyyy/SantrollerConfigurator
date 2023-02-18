using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class AreYouSureWindowViewModel : ReactiveObject
{
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();

    public AreYouSureWindowViewModel(string yesText, string noText, string text)
    {
        YesText = yesText;
        NoText = noText;
        Text = text;
        YesCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = true;
            return CloseWindowInteraction.Handle(new Unit());
        });
        NoCommand = ReactiveCommand.CreateFromObservable(() =>
        {
            Response = false;
            return CloseWindowInteraction.Handle(new Unit());
        });
    }

    public ICommand YesCommand { get; }
    public ICommand NoCommand { get; }
    public bool Response { get; set; }
    public string YesText { get; }
    public string NoText { get; }
    public string Text { get; }
}