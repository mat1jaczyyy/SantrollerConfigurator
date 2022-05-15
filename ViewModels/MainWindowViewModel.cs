using System;
using System.Reactive;
using GuitarConfiguratorSharp.Utils;
using ReactiveUI;

namespace GuitarConfiguratorSharp.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        // The Router associated with this Screen.
        // Required by the IScreen interface.
        public RoutingState Router { get; } = new RoutingState();

        // The command that navigates a user to first view model.
        public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

        // The command that navigates a user back.
        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

        public double _progress = 0;

        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _progress, value);
            }
        }

        public string _message = "Connected";

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _message, value);
            }
        }

        public MainWindowViewModel()
        {
            // Manage the routing state. Use the Router.Navigate.Execute
            // command to navigate to different view models. 
            //
            // Note, that the Navigate.Execute method accepts an instance 
            // of a view model, this allows you to pass parameters to 
            // your view models, or to reuse existing view models.
            //
            GoNext = ReactiveCommand.CreateFromObservable(
                () => Router.Navigate.Execute(new ConfigViewModel(this))
            );

            var pio = new PlatformIO();
            // TODO: the plan here is that we shove shared stuff into this class (such as handling talking to devices, platformio and all that)
            // And then this class can just expose the current device we are talking too, and the config screen can grab that and use it when programming
            // We can have it so that the initial screen also functions as the programming screen, as we have the progress bar on every page now so we don't have to worry.
            // Whats kinda neat is we can actually let the user start configuring even when we are doing the initial program too, since we will have a "Program" button that is just disabled
            // If something is going on.
            // This also means we can put device status in the top left corner again like the old version, and we can do that from here so it persists across versions.
            if (!pio.ready) {
                // Show a progress screen that shows what is currently happening
            }
            // pio.ProgressChanged += (val, val2, message) => Console.WriteLine($"{val} {val2} {message}");
            pio.ProgressChanged += (message, val, val2) => {
                this.Message = message;
                this.Progress = val2;
                Console.WriteLine(message);
            };
            // pio.TextChanged += (val, a) => Console.WriteLine(val);
            // pio.PlatformIOReady += () => {
            //     _ = pio.RunPlatformIO(null,"run","Initial Compile - ",0,0,100);
            // };
            _ = pio.InitialisePlatformIO();
        }
    }
}