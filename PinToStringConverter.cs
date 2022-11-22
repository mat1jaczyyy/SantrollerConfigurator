using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore;

public class PinToStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[2] == null || values[3] == null)
            return null;
        if (values[0] is not int || values[1] is not Microcontroller || values[2] is not ConfigViewModel || values[3] is not int) return null;
        var pin = (int) values[0]!;
        var microcontroller = (Microcontroller) values[1]!;
        var model = (ConfigViewModel) values[2]!;
        var selectedPin = (int) values[3]!;
        return microcontroller.GetPin(pin, selectedPin, model.Bindings);
    }
}