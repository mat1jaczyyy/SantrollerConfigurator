using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Input;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using Humanizer;

namespace GuitarConfigurator.NetCore;

public class EnumToStringConverter : IValueConverter
{
    public static string Convert(object? value)
    {
        if (value is Key key)
        {
            return KeyboardButton.Keys[key];
        }
        var valueType = value!.GetType();
        var fieldInfo = valueType.GetField(value!.ToString()!, BindingFlags.Static | BindingFlags.Public)!;
        var attributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Length > 0)
        {
            return attributes[0].Description;
        }
        return fieldInfo.Name.Humanize();
    }
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? null : Convert(value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}