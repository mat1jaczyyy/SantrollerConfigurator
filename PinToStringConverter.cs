using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore;

public class PinToStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[2] == null || values[3] == null || values[4] == null)
            return null;
        if (values[0] is not int || values[1] is not Microcontroller || values[2] is not ConfigViewModel ||
            values[3] is not int || values[4] is not (Output or Input or ConfigViewModel)) return null;
        var pin = (int) values[0]!;
        var selectedPin = (int) values[3]!;
        var microcontroller = (Microcontroller) values[1]!;
        var model = (ConfigViewModel) values[2]!;
        var twi = values[4] is ITwi twiIo && twiIo.TwiPins().Contains(pin);
        var spi = values[4] is ISpi spiIo && spiIo.SpiPins().Contains(pin) || values[4] is ConfigViewModel;

        var configs = values[4] switch
        {
            Input input => input.PinConfigs,
            Output output => output.GetPinConfigs(),
            ConfigViewModel => model.PinConfigs,
            _ => new List<PinConfig>()
        };
        return microcontroller.GetPin(pin, selectedPin, model.Bindings, twi, spi, configs, model,
            values[5] is ComboBoxItem);
    }
}