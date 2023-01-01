using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Views
{
    public class RaiseIssueWindow : ReactiveWindow<RaiseIssueWindowViewModel>
    {
        public RaiseIssueWindow()
        {
            this.WhenActivated(disposables =>
            {
                disposables(ViewModel!.CloseWindowInteraction.RegisterHandler(context =>
                {
                    context.SetOutput(new());
                    Close();
                }));
            });
            AvaloniaXamlLoader.Load(this);
        }
    }
}