using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views
{
    public class AreYouSureWindow : ReactiveWindow<AreYouSureWindowViewModel>
    {
        public AreYouSureWindow()
        {
            this.WhenActivated(disposables =>
            {
                disposables(ViewModel!.CloseWindowInteraction.RegisterHandler(context =>
                {
                    context.SetOutput(context.Input);
                    Close();
                }));
            });
            AvaloniaXamlLoader.Load(this);
        }
    }
}