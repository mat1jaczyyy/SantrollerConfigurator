using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using MouseButton = Avalonia.Input.MouseButton;

namespace GuitarConfigurator.NetCore;

public class InputImageConverter : IMultiValueConverter
{
    private static readonly Dictionary<object, Bitmap> Icons = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null)
            return null;

        if (values[1] is not DeviceControllerType) return null;
        var deviceControllerType = (DeviceControllerType) values[1]!;
        var name = values[0]!.GetType().Name + values[0] + deviceControllerType;
        if (Icons.TryGetValue(name, out var image)) return image;

        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var path = values[0] switch
        {
            "Empty" => "Generic",
            SimpleType type => "Combined/" + type switch
            {
                SimpleType.WiiInputSimple => "Wii",
                SimpleType.Ps2InputSimple => "PS2",
                SimpleType.WtNeckSimple => "GHWT",
                SimpleType.Gh5NeckSimple => "GH5",
                SimpleType.DjTurntableSimple => "DJ",
                SimpleType.UsbHost => "Usb",
                SimpleType.Bluetooth => "Bluetooth",
                SimpleType.Led => "Led",
                SimpleType.Rumble => "Rumble",
                SimpleType.ConsoleMode => "Console",
                _ => null
            },
            
            InputType.AnalogPinInput => "Combined/Analog",
            InputType.MultiplexerInput => "Combined/Multiplexer",
            InputType.DigitalPinInput => "Combined/Digital",
            InputType.MacroInput => "Combined/Macro",
            InputType.ConstantInput => "Combined/Constant",
            InputType.WiiInput => "Combined/Wii",
            InputType.Ps2Input => "Combined/PS2",
            InputType.TurntableInput => "Combined/DJ",
            InputType.WtNeckInput => "Combined/GHWT",
            InputType.Gh5NeckInput => "Combined/GH5",
            InputType.UsbHostInput => "Combined/Usb",
            DpadType type => (type.ToString().StartsWith("Ps2") ? "PS2/DPad" : "Wii/ClassicDPad") +
                             type.ToString()[3..],
            UsbHostInputType type => $"Combined/Usb/{type}",
            Key => "Keyboard",
            MouseAxis => "Mouse",
            MouseButton => "Mouse",
            DjAxisType type => "DJ/" + type.ToString().Replace("Left","").Replace("Right",""),
            DjInputType type => "DJ/" + type.ToString().Replace("Left","").Replace("Right",""),
            Ps2InputType type => "PS2/" + type.ToString().Replace("Dualshock2",""),
            WiiInputType type => "Wii/" + type,
            DrumAxisType type => deviceControllerType + "/" + type,
            GuitarAxisType type => deviceControllerType + "/" + type,
            InstrumentButtonType type => deviceControllerType + "/" + type,
            Gh5NeckInputType type => "GuitarHero/" + type,
            GhWtInputType type => "GuitarHero/" + type,
            EmulationModeType type => type switch
            {
                EmulationModeType.XboxOne => "Combined/XboxOne",
                EmulationModeType.Xbox360 => "Combined/Xbox360",
                EmulationModeType.Wii => "Combined/Wii",
                EmulationModeType.Ps3 => "Combined/PS3",
                EmulationModeType.Ps4Or5 => "Combined/PS4",
                EmulationModeType.Switch => "Combined/Switch",
                _ => throw new ArgumentOutOfRangeException()
            },
            StandardButtonType type => deviceControllerType switch
            {
                DeviceControllerType.Gamepad => $"Xbox360/{type}",
                DeviceControllerType.DancePad => $"Xbox360/{type}",
                DeviceControllerType.StageKit => $"Xbox360/{type}",
                DeviceControllerType.LiveGuitar => $"LiveGuitar/{type}",
                DeviceControllerType.Turntable => $"Xbox360/{type}",
                _ => $"{deviceControllerType}/{type}"
            },
            StandardAxisType type => deviceControllerType switch
            {
                DeviceControllerType.Gamepad => $"Xbox360/{type}",
                DeviceControllerType.DancePad => $"Xbox360/{type}",
                DeviceControllerType.StageKit => $"Xbox360/{type}",
                DeviceControllerType.LiveGuitar => $"LiveGuitar/{type}",
                DeviceControllerType.Turntable => $"Xbox360/{type}",
                _ => $"{deviceControllerType}/{type}"
            },
            Ps3AxisType type => "PS2/" + type.ToString().Replace("Pressure",""),
            _ => null
        };
        if (path == null) return null;
        try
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{path}.png"));
            var bitmap = new Bitmap(asset);
            Icons.Add(name, bitmap);
            return bitmap;
        }
        catch (FileNotFoundException)
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/Generic.png"));
            var bitmap = new Bitmap(asset);
            Icons.Add(name, bitmap);
            return bitmap;
        }
    }
}