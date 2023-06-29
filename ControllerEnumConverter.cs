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
    private static readonly Dictionary<StandardAxisType, string> AxisLabelsStandard =
        new()
        {
            {
                StandardAxisType.LeftStickX,
                "Left Joystick X Axis"
            },
            {
                StandardAxisType.LeftStickY,
                "Left Joystick Y Axis"
            },
            {
                StandardAxisType.RightStickX,
                "Right Joystick X Axis"
            },
            {
                StandardAxisType.RightStickY,
                "Right Joystick Y Axis"
            },
            {
                StandardAxisType.LeftTrigger,
                "Left Trigger Axis"
            },
            {
                StandardAxisType.RightTrigger,
                "Right Trigger Axis"
            }
        };

    private static readonly Dictionary<Tuple<DeviceControllerType, StandardButtonType>, string>
        ButtonLabels =
            new()
            {
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Guitar,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Guitar,
                        StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Guitar,
                        StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Guitar,
                        StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Guitar,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.Start),
                    "Start / Hero Power Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.LeftThumbClick),
                    "GHTV Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        StandardButtonType.Back),
                    "Select / Pause Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.A),
                    "A Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.B),
                    "B Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.X),
                    "X Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.Y),
                    "Y Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.LeftThumbClick),
                    "Left Stick Click"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.RightThumbClick),
                    "Right Stick Click"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.LeftShoulder),
                    "Left Bumper"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.RightShoulder),
                    "Right Bumper"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Gamepad,
                        StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.A),
                    "A Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.B),
                    "B Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.X),
                    "X Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.Y),
                    "Y Button / Euphoria Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, StandardButtonType>(DeviceControllerType.Turntable,
                        StandardButtonType.DpadRight),
                    "D-pad Right"
                }
            };
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[2] == null)
            return null;

        if (values[0] is not Enum) return null;

        if (values[1] is not DeviceControllerType || values[2] is not RhythmType) return null;

        var deviceControllerType = (DeviceControllerType) values[1]!;
        switch (values[0])
        {
            case StandardAxisType axis:
                return GetAxisText(deviceControllerType, axis);
            case StandardButtonType button:
                return GetButtonText(deviceControllerType, button);
        }

        // Maybe we just change out all this nice description stuff for something else
        var valueType = values[0]!.GetType();
        var fieldInfo = valueType.GetField(values[0]!.ToString()!, BindingFlags.Static | BindingFlags.Public)!;
        var attributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : fieldInfo.Name;
    }

    public static string GetAxisText(DeviceControllerType deviceControllerType,
        StandardAxisType axis)
    {
        // All the instruments handle their own axis'
        return deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick
            or DeviceControllerType.DancePad or DeviceControllerType.FlightStick or DeviceControllerType.Gamepad
            ? AxisLabelsStandard[axis]
            : "";
    }

    public static string GetButtonText(DeviceControllerType deviceControllerType,
        StandardButtonType button)
    {
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick or DeviceControllerType.FlightStick)
            deviceControllerType = DeviceControllerType.Gamepad;
        // Turntable mappings hide extra buttons like bumpers and stick click
        if (deviceControllerType is DeviceControllerType.DancePad  or DeviceControllerType.Drum
            or DeviceControllerType.StageKit)
            deviceControllerType = DeviceControllerType.Turntable;
        return ButtonLabels.GetValueOrDefault(
            new Tuple<DeviceControllerType, StandardButtonType>(deviceControllerType, button), "");
    }

    public static (List<Output>, List<object>) FilterValidOutputs(DeviceControllerType controllerType,
        RhythmType rhythmType, IEnumerable<Output> outputs)
    {
        var types = GetTypes((controllerType, rhythmType))
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

    public static IEnumerable<object> GetTypes((DeviceControllerType deviceControllerType, RhythmType rhythmType) arg)
    {
        var deviceControllerType = arg.deviceControllerType;
        var rhythmType = arg.rhythmType;
        var otherBindings = deviceControllerType switch
        {
            DeviceControllerType.Drum => DrumAxisTypeMethods.GetTypeFor(rhythmType).Cast<object>(),
            DeviceControllerType.Gamepad => Enum.GetValues<Ps3AxisType>().Cast<object>()
                .Concat(AxisLabelsStandard.Keys.Cast<object>()),
            DeviceControllerType.Turntable => Enum.GetValues<DjInputType>()
                .Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
                .Cast<object>()
                .Concat(Enum.GetValues<DjAxisType>().Cast<object>()),
            DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar => GuitarAxisTypeMethods
                .GetTypeFor(deviceControllerType, rhythmType)
                .Cast<object>()
                .Concat(InstrumentButtonTypeExtensions.GetButtons(deviceControllerType, rhythmType).Cast<object>()),
            _ => AxisLabelsStandard.Keys.Cast<object>()
        };
        // Most devices, except for the Guitars actually use standard button bindings, and then may have additional special buttons which are handled above.
        if (deviceControllerType is not (DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar or DeviceControllerType.Turntable or DeviceControllerType.DancePad or DeviceControllerType.Drum or DeviceControllerType.StageKit))
            deviceControllerType = DeviceControllerType.Gamepad;
        // Though a good chunk need a reduced set of standard bindings
        if (deviceControllerType is DeviceControllerType.DancePad or DeviceControllerType.Drum or DeviceControllerType.StageKit)
            deviceControllerType = DeviceControllerType.Turntable;
        return Enum.GetValues<SimpleType>().Cast<object>()
            .Concat(otherBindings)
            .Concat(Enum.GetValues<StandardButtonType>()
                .Where(type =>
                    ButtonLabels.ContainsKey(
                        new Tuple<DeviceControllerType, StandardButtonType>(deviceControllerType, type)))
                .Cast<object>());
    }
}