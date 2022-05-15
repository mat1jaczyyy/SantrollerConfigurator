using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Device.Net;
using Device.Net.LibUsb;
using Usb.Net;
using GuitarConfiguratorSharp.Utils;
using ReactiveUI;
using System.ComponentModel;

namespace GuitarConfiguratorSharp.ViewModels
{
    public class ConfigViewModel : ReactiveObject, IRoutableViewModel
    {
        public static readonly Guid ControllerGUID = new("DF59037D-7C92-4155-AC12-7D700A313D78");

        public static readonly FilterDeviceDefinition ArdwiinoDeviceFilter =
            new(0x1209, 0x2882, label: "Ardwiino",
                classGuid: ControllerGUID);
        public ObservableCollection<PinBinding> Bindings { get; }

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public IScreen HostScreen { get; }

        async Task Test()
        {
            var usbFactory = ArdwiinoDeviceFilter.CreateLibUsbDeviceFactory();
            var deviceDefinitions = (await usbFactory.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false)).ToList();
            Console.WriteLine(deviceDefinitions[0]);
            IUsbDevice dev = (IUsbDevice)(await usbFactory.ConnectFirstAsync());

            // dev.PerformControlTransferAsync()
        }

        public void RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public ConfigViewModel(IScreen screen)
        {
            HostScreen = screen;
                        
            Bindings = new()
            {
                new PinBinding(0, "Green", "/Assets/Icons/GH/whammy2.png", null, false, 1, false, Colors.Green, Colors.Black),
                new PinBinding(0, "Green", "/Assets/Icons/GH/green_fret2.png", null, false, 1, false, Colors.Green, Colors.Black),
                new PinBinding(1, "Red", "/Assets/Icons/GH/red_fret2.png", null, false,1, false, Colors.Red, Colors.Black),
                new PinBinding(2, "Yellow", "/Assets/Icons/GH/yellow_fret2.png", null, false,1, false, Colors.Yellow, Colors.Black),
                new PinBinding(3, "Blue", "/Assets/Icons/GH/blue_fret2.png", null, false,1, false, Colors.Blue, Colors.Black),
                new PinBinding(4, "Orange", "/Assets/Icons/GH/orange_fret2.png", null, false,1, false, Colors.Orange, Colors.Black),
                new PinBinding(5, "DPad Up", "/Assets/Icons/GH/strum_up2.png", null, false,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "DPad Down", "/Assets/Icons/GH/strum_down2.png", null, true,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "DPad Down", "/Assets/Icons/GH/start2.png", null, true,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "DPad Up", "/Assets/Icons/GH/select2.png", null, false,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "DPad Left", "/Assets/Icons/Others/Xbox360/360_Dpad_Left.png", null, false,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "DPad Right", "/Assets/Icons/Others/Xbox360/360_Dpad_Right.png", null, true,1, false, Colors.Black, Colors.Black),
                new PinBinding(5, "Joystick Left Horizontal", "/Assets/Icons/Others/Xbox360/360_Left_Stick_X.png", "Map Joystick\nto DPad", true,1, true, Colors.Black, Colors.Black),
                new PinBinding(5, "Joystick Left Vertical", "/Assets/Icons/Others/Xbox360/360_Left_Stick_Y.png", "Map Joystick\nto DPad", true,1, true, Colors.Black, Colors.Black),
                new PinBinding(5, "Joystick Right Horizontal", "/Assets/Icons/Others/Xbox360/360_Right_Stick_X.png", "Map Joystick\nto DPad", true,1, true, Colors.Black, Colors.Black),
                new PinBinding(5, "Joystick Right Vertical", "/Assets/Icons/Others/Xbox360/360_Right_Stick_Y.png", "Map Joystick\nto DPad", true,1, true, Colors.Black, Colors.Black)
            };
        }
    }
}