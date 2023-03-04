using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;

namespace GuitarConfigurator.NetCore;

public class ControllerEnumConverter : IMultiValueConverter
{
    private static readonly Dictionary<Tuple<DeviceControllerType, RhythmType?, StandardAxisType>, string> AxisLabels =
        new()
        {
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.LeftStickX),
                "Left Joystick X Axis"
            },
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.LeftStickY),
                "Left Joystick Y Axis"
            },
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.RightStickX),
                "Right Joystick X Axis"
            },
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.RightStickY),
                "Right Joystick Y Axis"
            },
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.LeftTrigger),
                "Left Trigger Axis"
            },
            {
                new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(DeviceControllerType.Gamepad, null,
                    StandardAxisType.RightTrigger),
                "Right Trigger Axis"
            }
        };

    private static readonly Dictionary<Tuple<DeviceControllerType, RhythmType?, StandardButtonType>, string>
        ButtonLabels =
            new()
            {
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.A),
                    "Green Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.B),
                    "Red Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.X),
                    "Yellow Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.Y),
                    "Blue Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.LeftShoulder),
                    "Orange Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.DpadUp),
                    "Strum Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.DpadDown),
                    "Strum Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.GuitarHero, StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.A),
                    "Lower Green Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.B),
                    "Lower Red Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.X),
                    "Lower Yellow Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.Y),
                    "Lower Blue Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.LeftShoulder),
                    "Lower Orange Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.DpadUp),
                    "Strum Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.DpadDown),
                    "Strum Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Guitar,
                        RhythmType.RockBand, StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.A),
                    "Black 1 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.B),
                    "Black 2 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.Y),
                    "Black 3 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.X),
                    "Black 1 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.LeftShoulder),
                    "Black 2 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.RightShoulder),
                    "Black 3 Fret"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.DpadUp),
                    "Strum Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.DpadDown),
                    "Strum Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.LeftThumbClick),
                    "GHTV Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.LiveGuitar,
                        null, StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.A),
                    "A Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.B),
                    "B Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.X),
                    "X Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.Y),
                    "Y Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Turntable,
                        null, StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.A),
                    "A Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.B),
                    "B Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.X),
                    "X Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.Y),
                    "Y Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.LeftThumbClick),
                    "Left Stick Click"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.RightThumbClick),
                    "Right Stick Click"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.LeftShoulder),
                    "Left Bumper"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.RightShoulder),
                    "Right Bumper"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Gamepad, null,
                        StandardButtonType.DpadRight),
                    "D-pad Right"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.A),
                    "A Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.B),
                    "B Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.X),
                    "X Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.Y),
                    "Y Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.Start),
                    "Start Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.Back),
                    "Select Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.Guide),
                    "Home Button"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.DpadUp),
                    "D-pad Up"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.DpadDown),
                    "D-pad Down"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
                        StandardButtonType.DpadLeft),
                    "D-pad Left"
                },
                {
                    new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(DeviceControllerType.Drum, null,
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
        var rhythmType = (RhythmType) values[2]!;
        switch (values[0])
        {
            case StandardAxisType axis:
                return GetAxisText(deviceControllerType, rhythmType, axis);
            case StandardButtonType button:
                return GetButtonText(deviceControllerType, rhythmType, button);
        }
        var valueType = values[0]!.GetType();
        var fieldInfo = valueType.GetField(values[0]!.ToString()!, BindingFlags.Static | BindingFlags.Public)!;
        var attributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : fieldInfo.Name;
    }

    public static string? GetAxisText(DeviceControllerType deviceControllerType, RhythmType? rhythmType,
        StandardAxisType axis)
    {
        if (deviceControllerType is not DeviceControllerType.Guitar or DeviceControllerType.Drum)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick
            or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick or DeviceControllerType.Drum)
            deviceControllerType = DeviceControllerType.Gamepad;
        return AxisLabels.GetValueOrDefault(
            new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(deviceControllerType, rhythmType, axis));
    }

    public static string? GetButtonText(DeviceControllerType deviceControllerType, RhythmType? rhythmType,
        StandardButtonType button)
    {
        if (deviceControllerType is not DeviceControllerType.Guitar or DeviceControllerType.Drum)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick
            or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick or DeviceControllerType.Drum)
            deviceControllerType = DeviceControllerType.Gamepad;
        return ButtonLabels.GetValueOrDefault(
            new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(deviceControllerType, rhythmType, button));
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
                case RbButton button:
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
            }

        return (extra, types);
    }

    public static IEnumerable<object> GetTypes((DeviceControllerType deviceControllerType, RhythmType rhythmType) arg)
    {
        var deviceControllerType = arg.deviceControllerType;
        RhythmType? rhythmType = arg.rhythmType;
        var otherBindings = Enumerable.Empty<object>();
        otherBindings = deviceControllerType switch
        {
            DeviceControllerType.Drum => DrumAxisTypeMethods.GetTypeFor(rhythmType.Value).Cast<object>(),
            DeviceControllerType.Gamepad => Enum.GetValues<Ps3AxisType>().Cast<object>(),
            DeviceControllerType.Turntable => Enum.GetValues<DjInputType>()
                .Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
                .Cast<object>().Concat(Enum.GetValues<DjAxisType>().Cast<object>()),
            DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar => GuitarAxisTypeMethods
                .GetTypeFor(deviceControllerType, rhythmType.Value)
                .Cast<object>()
                .Concat(otherBindings),
            _ => otherBindings
        };
        if (deviceControllerType is DeviceControllerType.Guitar && rhythmType == RhythmType.RockBand)
            otherBindings = Enum.GetValues<RBButtonType>().Cast<object>().Concat(otherBindings);
        if (deviceControllerType is not DeviceControllerType.Guitar)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick
            or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick)
            deviceControllerType = DeviceControllerType.Gamepad;
        return Enum.GetValues<SimpleType>().Cast<object>()
            .Concat(otherBindings)
            .Concat(Enum.GetValues<StandardAxisType>()
                .Where(type =>
                    AxisLabels.ContainsKey(
                        new Tuple<DeviceControllerType, RhythmType?, StandardAxisType>(deviceControllerType, rhythmType,
                            type))).Cast<object>())
            .Concat(Enum.GetValues<StandardButtonType>()
                .Where(type =>
                    ButtonLabels.ContainsKey(
                        new Tuple<DeviceControllerType, RhythmType?, StandardButtonType>(deviceControllerType,
                            rhythmType, type))).Cast<object>());
    }
}