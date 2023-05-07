using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore;

public class PinToStringConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[3] == null)
            return null;
        if (values[0] is not int || values[1] is not ConfigViewModel ||
            values[3] is not (Output or Input or ConfigViewModel)) return null;
        var pin = (int) values[0]!;
        var selectedPin = -1;
        if (values[2] is not null) selectedPin = (int) values[2]!;
        var model = (ConfigViewModel) values[1]!;
        var microcontroller = model.Microcontroller;
        var twi = values[3] is ITwi;
        var spi = values[3] is ISpi || values[3] is ConfigViewModel;

        var configs = values[3] switch
        {
            Input input => input.PinConfigs,
            Output output => output.GetPinConfigs(),
            ConfigViewModel => model.PinConfigs,
            _ => new List<PinConfig>()
        };
        return microcontroller.GetPin(pin, selectedPin, model.Bindings.Items, twi, spi, configs, model,
            values[4] is ComboBoxItem);
    }
}