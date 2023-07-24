using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;

namespace GuitarConfigurator.NetCore;

public class ControllerEnumConverter : IMultiValueConverter
{
    private static readonly List<StandardButtonType> SupportedButtonsGuitar = new()
    {
        StandardButtonType.DpadLeft,
        StandardButtonType.DpadRight,
        StandardButtonType.Start,
        StandardButtonType.Back,
        StandardButtonType.Guide
    };

    private static readonly List<StandardButtonType> SupportedButtonsDrums = new()
    {
        StandardButtonType.DpadUp,
        StandardButtonType.DpadDown,
        StandardButtonType.DpadLeft,
        StandardButtonType.DpadRight,
        StandardButtonType.Start,
        StandardButtonType.Back,
        StandardButtonType.Guide
    };

    private static readonly List<StandardButtonType> SupportedButtonsNonGamepad = new()
    {
        StandardButtonType.A,
        StandardButtonType.B,
        StandardButtonType.X,
        StandardButtonType.Y,
        StandardButtonType.DpadUp,
        StandardButtonType.DpadDown,
        StandardButtonType.DpadLeft,
        StandardButtonType.DpadRight,
        StandardButtonType.Start,
        StandardButtonType.Back,
        StandardButtonType.Guide
    };

    private static readonly Dictionary<StandardButtonType, string> PlaystationLabels = new()
    {
        {StandardButtonType.A, "Cross Button"},
        {StandardButtonType.B, "Circle Button"},
        {StandardButtonType.X, "Square Button"},
        {StandardButtonType.Y, "Triangle Button"},
        {StandardButtonType.Guide, "PlayStation Button"}
    };
    private static readonly Dictionary<StandardButtonType, string> XboxLabels = new()
    {
        {StandardButtonType.Guide, "Guide Button"},
        {StandardButtonType.Back, "Back Button"},
    };

    private static readonly Dictionary<(DeviceControllerType, StandardButtonType), string>
        CustomButtonLabels =
            new()
            {
                {
                    (DeviceControllerType.LiveGuitar, StandardButtonType.Start),
                    "/ Hero Power Button"
                },
                {
                    (DeviceControllerType.LiveGuitar, StandardButtonType.LeftThumbClick),
                    "GHTV Button"
                },
                {
                    (DeviceControllerType.LiveGuitar, StandardButtonType.Back),
                    "/ Pause Button"
                },
                {
                    (DeviceControllerType.Turntable, StandardButtonType.Y),
                    "/ Euphoria Button"
                },
            };

    public static string Convert(Enum value, DeviceControllerType deviceType, LegendType legendType, bool swapSwitchFaceButtons)
    {
        if (!swapSwitchFaceButtons)
        {
            value = value switch
            {
                StandardButtonType.X => StandardButtonType.Y,
                StandardButtonType.Y => StandardButtonType.X,
                StandardButtonType.A => StandardButtonType.B,
                StandardButtonType.B => StandardButtonType.A,
                _ => value
            };
        }

        // Maybe we just change out all this nice description stuff for something else
        var valueType = value.GetType();
        var fieldInfo = valueType.GetField(value.ToString()!, BindingFlags.Static | BindingFlags.Public)!;
        var attributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        var ret = attributes.Length > 0 ? attributes[0].Description : fieldInfo.Name;
        if (value is StandardButtonType button)
        {
            
            if (legendType == LegendType.PlayStation && PlaystationLabels.TryGetValue(button, out var label))
            {
                ret = label;
            }
            if (legendType == LegendType.Xbox && XboxLabels.TryGetValue(button, out label))
            {
                ret = label;
            }

            if (CustomButtonLabels.TryGetValue((deviceType, button), out label))
            {
                if (label.StartsWith("/"))
                {
                    ret += " "+ label;
                }
                else
                {
                    ret = label;
                }
            }
        }

        return ret;
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not Enum e || values[1] is not DeviceControllerType t || values[2] is not LegendType l || values[4] is not bool swapSwitchFaceButtons) return null;
        return Convert(e, t, l, swapSwitchFaceButtons);
    }

    public static (List<Output>, List<object>) FilterValidOutputs(DeviceControllerType controllerType,
        IEnumerable<Output> outputs)
    {
        var types = GetTypes(controllerType)
            .Where(s => s is not SimpleType).ToList();
        var types2 = new List<object>(types);
        var extra = new List<Output>();
        foreach (var binding in outputs)
            switch (binding)
            {
                case ControllerButton button:
                    types.Remove(button.Type);
                    if (!types2.Contains(button.Type)) extra.Add(binding);
                    break;
                case GuitarButton button:
                    types.Remove(button.Type);
                    if (!types2.Contains(button.Type)) extra.Add(binding);
                    break;
                case ControllerAxis axis:
                    types.Remove(axis.Type);
                    if (!types2.Contains(axis.Type)) extra.Add(binding);
                    break;
                case GuitarAxis axis:
                    types.Remove(axis.Type);
                    if (!types2.Contains(axis.Type)) extra.Add(binding);
                    break;
                case DrumAxis axis:
                    types.Remove(axis.Type);
                    if (!types2.Contains(axis.Type)) extra.Add(binding);
                    break;
                case DjAxis axis:
                    types.Remove(axis.Type);
                    if (!types2.Contains(axis.Type)) extra.Add(binding);
                    break;
            }

        return (extra, types);
    }

    public static bool ButtonValid(StandardButtonType button, DeviceControllerType deviceControllerType)
    {
        if (CustomButtonLabels.ContainsKey((deviceControllerType, button)))
        {
            return true;
        }
        switch (deviceControllerType)
        {
            case DeviceControllerType.Gamepad:
                return true;
            case DeviceControllerType.Turntable:
            case DeviceControllerType.DancePad:
            case DeviceControllerType.StageKit:
                return SupportedButtonsNonGamepad.Contains(button);
            case DeviceControllerType.LiveGuitar:
            case DeviceControllerType.GuitarHeroGuitar:
            case DeviceControllerType.RockBandGuitar:
                return SupportedButtonsGuitar.Contains(button);
            case DeviceControllerType.GuitarHeroDrums:
            case DeviceControllerType.RockBandDrums:
                return SupportedButtonsDrums.Contains(button);
            default:
                return true;
        }
    }

    public static IEnumerable<object> GetTypes(DeviceControllerType deviceType)
    {
        var otherBindings = deviceType switch
        {
            DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandDrums =>
                DrumAxisTypeMethods.GetTypeFor(deviceType).Cast<object>(),
            DeviceControllerType.Gamepad => Enum.GetValues<Ps3AxisType>().Cast<object>()
                .Concat(Enum.GetValues<StandardAxisType>().Cast<object>()),
            DeviceControllerType.Turntable => Enum.GetValues<DjInputType>()
                .Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
                .Cast<object>()
                .Concat(Enum.GetValues<DjAxisType>().Cast<object>()),
            DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar
                or DeviceControllerType.LiveGuitar => GuitarAxisTypeMethods
                    .GetTypeFor(deviceType)
                    .Cast<object>()
                    .Concat(InstrumentButtonTypeExtensions.GetButtons(deviceType).Cast<object>()),
            _ => Enum.GetValues<StandardAxisType>().Cast<object>()
        };
        return Enum.GetValues<SimpleType>().Cast<object>()
            .Concat(otherBindings)
            .Concat(Enum.GetValues<StandardButtonType>()
                .Where(type => ButtonValid(type, deviceType))
                .Cast<object>());
    }
}