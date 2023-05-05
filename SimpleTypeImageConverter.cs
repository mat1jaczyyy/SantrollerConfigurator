using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using Humanizer;

namespace GuitarConfigurator.NetCore;

public class SimpleTypeImageConverter : IValueConverter
{
    private static Dictionary<object, Bitmap> _icons = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;
        var name = value.GetType().Name + value;
        if (_icons.TryGetValue(name, out var image))
        {
            return image;
        }

        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var path = value switch
        {
            SimpleType simpleType => "Combined/" + simpleType switch
            {
                SimpleType.WiiInputSimple => "Wii",
                SimpleType.Ps2InputSimple => "PS2",
                SimpleType.WtNeckSimple => "GHWT",
                SimpleType.Gh5NeckSimple => "GH5",
                SimpleType.DjTurntableSimple => "DJ",
                // TODO:
                // SimpleType.UsbHost => "Usb.png",
                // SimpleType.Bluetooth => "Bluetooth.png",
                // SimpleType.RfSimple => "Rf.png",
                // SimpleType.Led => "Led.png",
                // SimpleType.Rumble => "Rumble.png",
                // SimpleType.ConsoleMode => "Console.png",
                _ => "../None.png"
            },
            // TODO: these
            DjAxisType djAxisType => "dj/" + djAxisType,
            DrumAxisType drumAxisType => "drum/" + drumAxisType,
            // TODO: rb +  ghl use different icons
            GuitarAxisType guitarAxisType => "GuitarHero/" + guitarAxisType,
            InstrumentButtonType instrumentButtonType => "GuitarHero/" + instrumentButtonType,
            StandardButtonType standardButtonType => "Others/Xbox360/360_" + standardButtonType,
            StandardAxisType standardAxisType => "Others/Xbox360/360_" + standardAxisType,
            Ps3AxisType ps3AxisType => "PS3/PS3_" + ps3AxisType,
            _ => null
        };
        
        Console.WriteLine(path);

        if (path == null)
        {
            return null;
        }
        try
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{path}.png"));
            var bitmap = new Bitmap(asset);
            _icons.Add(name, bitmap);
            return bitmap;
        }
        catch (FileNotFoundException)
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/Generic.png"));
            var bitmap = new Bitmap(asset);
            _icons.Add(name, bitmap);
            return bitmap;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}