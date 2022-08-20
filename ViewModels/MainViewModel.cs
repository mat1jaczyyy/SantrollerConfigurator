using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using ReactiveUI;
using System.ComponentModel;
using GuitarConfiguratorSharp.Configuration;

namespace GuitarConfiguratorSharp.ViewModels
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