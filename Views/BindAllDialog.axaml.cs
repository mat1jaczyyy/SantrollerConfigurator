using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Views
{
    public class BindAllWindow : ReactiveWindow<BindAllWindowViewModel>
    {
        public BindAllWindow()
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