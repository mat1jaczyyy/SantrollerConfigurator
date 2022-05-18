using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.ViewModels;
using ReactiveUI;
namespace GuitarConfiguratorSharp.Views
{
    public partial class MainView : ReactiveUserControl<MainViewModel>
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}