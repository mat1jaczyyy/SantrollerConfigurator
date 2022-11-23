using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore;

public class PinToStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[2] == null || values[3] == null)
            return null;
        if (values[0] is not int || values[1] is not Microcontroller || values[2] is not ConfigViewModel || values[3] is not int || values[4] is not (Output or Input)) return null;
        var pin = (int) values[0]!;
        var selectedPin = (int) values[3]!;
        var microcontroller = (Microcontroller) values[1]!;
        var model = (ConfigViewModel) values[2]!;
        var twi = values[4] is ITwi twiIo && twiIo.TwiPins().Contains(pin);
        var spi = values[4] is ISpi spiIo && spiIo.SpiPins().Contains(pin);
       
        var configs = values[4] switch
        {
            Input input => input.PinConfigs,
            Output output => output.GetPinConfigs(),
            _ => new List<PinConfig>()
        };
        return microcontroller.GetPin(pin, selectedPin, model.Bindings, twi, spi, configs);
    }
}