using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels;

public class AreYouSureWindowViewModel : ReactiveObject
{
    public ICommand YesCommand { get; }
    public ICommand NoCommand { get; }
    public readonly Interaction<Unit, Unit> CloseWindowInteraction = new();
    public bool Response { get; set; }
    public string YesText { get; }
    public string NoText { get; }
    public string Text { get; }

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
}