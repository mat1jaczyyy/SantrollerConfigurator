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
        if (values[0] == null || values[1] == null || values[2] == null)
            return null;

        if (values[1] is not DeviceControllerType || values[2] is not RhythmType) return null;
        var deviceControllerType = (DeviceControllerType) values[1]!;
        var rhythmType = (RhythmType) values[2]!;
        var name = values[0]!.GetType().Name + values[0] + rhythmType + deviceControllerType;
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
            
            InputType.AnalogPinInput => "Generic",
            InputType.MultiplexerInput => "Generic",
            InputType.DigitalPinInput => "Generic",
            InputType.MacroInput => "Generic",
            InputType.WiiInput => "Combined/Wii",
            InputType.Ps2Input => "Combined/PS2",
            InputType.TurntableInput => "Combined/DJ",
            InputType.WtNeckInput => "Combined/GHWT",
            InputType.Gh5NeckInput => "Combined/GH5",
            InputType.UsbHostInput => "Combined/Usb",
            DpadType type => (type.ToString().StartsWith("Ps2") ? "PS2/DPad" : "Wii/ClassicDPad") +
                             type.ToString()[3..],
            UsbHostInputType => "Combined/Usb",
            Key => "Keyboard",
            MouseAxis => "Mouse",
            MouseButton => "Mouse",
            DjAxisType type => "dj/" + type,
            DjInputType type => "dj/" + type,
            Ps2InputType type => "PS2/" + type,
            WiiInputType type => "Wii/" + type,
            DrumAxisType type => rhythmType + "/" + type,
            GuitarAxisType type => rhythmType + "/" + type,
            InstrumentButtonType type => rhythmType + "/" + type,
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
                DeviceControllerType.Gamepad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.ArcadeStick => $"Others/Xbox360/360_{type}",
                DeviceControllerType.FlightStick => $"Others/Xbox360/360_{type}",
                DeviceControllerType.DancePad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.ArcadePad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.StageKit => $"Others/Xbox360/360_{type}",
                DeviceControllerType.Guitar => $"{rhythmType}/{type}",
                DeviceControllerType.Drum => $"{rhythmType}/{type}",
                DeviceControllerType.LiveGuitar => $"GuitarHero/{type}",
                DeviceControllerType.Turntable => $"DJ/{type}",
                _ => null
            },
            StandardAxisType type => deviceControllerType switch
            {
                DeviceControllerType.Gamepad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.ArcadeStick => $"Others/Xbox360/360_{type}",
                DeviceControllerType.FlightStick => $"Others/Xbox360/360_{type}",
                DeviceControllerType.DancePad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.ArcadePad => $"Others/Xbox360/360_{type}",
                DeviceControllerType.StageKit => $"Others/Xbox360/360_{type}",
                DeviceControllerType.Guitar => $"{rhythmType}/{type}",
                DeviceControllerType.Drum => $"{rhythmType}/{type}",
                DeviceControllerType.LiveGuitar => $"GuitarHero/{type}",
                DeviceControllerType.Turntable => $"DJ/{type}",
                _ => null
            },
            Ps3AxisType type => "PS3/PS3_" + type,
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