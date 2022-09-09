using System;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    public class MainViewModel : ReactiveObject, IRoutableViewModel
    {

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        public MainWindowViewModel Main { get; }

        public MainViewModel(MainWindowViewModel screen)
        {
            Main = screen;
            HostScreen = screen;
        }
    }
}