using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore;

public class ControllerEnumConverter : IMultiValueConverter
{
    private static readonly Dictionary<Tuple<DeviceControllerType, RhythmType?, StandardAxisType>, string> AxisLabels =
        new()
        {
            {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardAxisType.LeftStickX), "Joystick X Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardAxisType.LeftStickY), "Joystick Y Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardAxisType.RightStickX), "Whammy Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardAxisType.RightStickY), "Tilt Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardAxisType.LeftStickX), "Joystick X Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardAxisType.LeftStickY), "Joystick Y Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardAxisType.RightStickX), "Whammy Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardAxisType.RightStickY), "Tilt Axis"},
            {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardAxisType.LeftTrigger), "Effects Switch"},
            {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardAxisType.LeftStickX), "Joystick X Axis"},
            {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardAxisType.LeftStickY), "Joystick Y Axis"},
            {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardAxisType.LeftStickX), "Joystick X Axis"},
            {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardAxisType.LeftStickY), "Joystick Y Axis"},
            {new(DeviceControllerType.LiveGuitar, null, StandardAxisType.LeftStickX), "Joystick X Axis"},
            {new(DeviceControllerType.LiveGuitar, null, StandardAxisType.LeftStickY), "Joystick Y Axis"},
            {new(DeviceControllerType.LiveGuitar, null, StandardAxisType.RightStickX), "Whammy Axis"},
            {new(DeviceControllerType.LiveGuitar, null, StandardAxisType.RightStickY), "Tilt Axis"},
            {new(DeviceControllerType.TurnTable, null, StandardAxisType.RightStickX), "Crossfader"},
            {new(DeviceControllerType.TurnTable, null, StandardAxisType.RightStickY), "Effects knob"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.LeftStickX), "Left Joystick X Axis"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.LeftStickY), "Left Joystick Y Axis"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.RightStickX), "Right Joystick X Axis"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.RightStickY), "Right Joystick Y Axis"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.LeftTrigger), "Left Trigger Axis"},
            {new(DeviceControllerType.Gamepad, null, StandardAxisType.RightTrigger), "Right Trigger Axis"},
        };

    private static readonly Dictionary<Tuple<DeviceControllerType, RhythmType?, StandardButtonType>, string>
        ButtonLabels =
            new()
            {
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.A), "Green Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.B), "Red Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Y), "Yellow Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.X), "Blue Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Lb), "Orange Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Up), "Strum Up"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Down), "Strum Down"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.Guitar, RhythmType.GuitarHero, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.A), "Green Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.B), "Red Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Y), "Yellow Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.X), "Blue Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Lb), "Orange Fret"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Up), "Strum Up"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Down), "Strum Down"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.Guitar, RhythmType.RockBand, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.A), "Black 1 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.B), "Black 2 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Y), "Black 3 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.X), "Black 1 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Lb), "Black 2 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Rb), "Black 3 Fret"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Up), "Strum Up"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Down), "Strum Down"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.LiveGuitar, null, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.A), "A Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.B), "B Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Y), "Y Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.X), "X Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.LeftStick), "Euphoria Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Up), "D-pad Up"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Down), "D-pad Down"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.TurnTable, null, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.A), "Green Drum"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.B), "Red Drum"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Y), "Yellow Drum"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.X), "Blue Cymbal"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Lb), "Kick Pedal"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Rb), "Orange Cymbal"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Up), "D-pad Up"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Down), "D-pad Down"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.Drum, RhythmType.GuitarHero, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.A), "Green Drum"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.B), "Red Drum"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Y), "Yellow Drum"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.X), "Blue Drum"},
                // {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.A | StandardButtonType.RightClick ), "Green Cymbal"},
                // {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Y | StandardButtonType.RightClick ), "Yellow Cymbal"},
                // {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.X | StandardButtonType.RightClick ), "Blue Cymbal"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Lb), "Kick Pedal"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.LeftStick), "Hi-Hat Pedal"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Up), "D-pad Up"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Down), "D-pad Down"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Right), "D-pad Right"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.Drum, RhythmType.RockBand, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.A), "A Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.B), "B Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.X), "X Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Y), "Y Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.LeftStick), "Left Stick Click"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.RightStick), "Right Stick Click"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Start), "Start Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Select), "Select Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Home), "Home Button"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Lb), "Left Bumper"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Rb), "Right Bumper"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Up), "D-pad Up"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Down), "D-pad Down"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Left), "D-pad Left"},
                {new(DeviceControllerType.Gamepad, null, StandardButtonType.Right), "D-pad Right"},
            };
    public static string? GetAxisText(DeviceControllerType deviceControllerType, RhythmType? rhythmType, StandardAxisType axis) {
        if (deviceControllerType is not DeviceControllerType.Guitar or DeviceControllerType.Drum)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick)
            deviceControllerType = DeviceControllerType.Gamepad;
        return AxisLabels.GetValueOrDefault(new(deviceControllerType, rhythmType, axis));
    }
    public static string? GetButtonText(DeviceControllerType deviceControllerType, RhythmType? rhythmType,
        StandardButtonType button) {
        if (deviceControllerType is not DeviceControllerType.Guitar or DeviceControllerType.Drum)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick)
            deviceControllerType = DeviceControllerType.Gamepad;
        return ButtonLabels.GetValueOrDefault(new(deviceControllerType, rhythmType, button));
    }
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] == null || values[2] == null)
            return null;

        if (values[0] is not Enum)
        {
            return null;
        }

        if (values[1] is not DeviceControllerType || values[2] is not RhythmType)
        {
            return null;
        }

        var deviceControllerType = (DeviceControllerType) values[1]!;
        var rhythmType = (RhythmType) values[2]!;
        if (values[0] is StandardAxisType axis)
        {
            return GetAxisText(deviceControllerType, rhythmType, axis);
        }

        if (values[0] is StandardButtonType button)
        {
            return GetButtonText(deviceControllerType, rhythmType, button);
        }

        var valueType = values[0]!.GetType();
        var fieldInfo = valueType.GetField(values[0]!.ToString()!, BindingFlags.Static | BindingFlags.Public)!;
        var attributes = (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Length > 0)
        {
            return attributes[0].Description;
        }

        return fieldInfo.Name;
    }

    public static IEnumerable<object> GetTypes((DeviceControllerType, RhythmType) arg)
    {
        var deviceControllerType = arg.Item1;
        RhythmType? rhythmType = arg.Item2;
        if (deviceControllerType is not DeviceControllerType.Guitar or DeviceControllerType.Drum)
            rhythmType = null;
        if (deviceControllerType is DeviceControllerType.ArcadePad or DeviceControllerType.ArcadeStick or DeviceControllerType.DancePad
            or DeviceControllerType.Wheel or DeviceControllerType.FlightStick)
            deviceControllerType = DeviceControllerType.Gamepad;
        return Enum.GetValues<SimpleType>().Cast<object>()
            .Concat(Enum.GetValues<StandardAxisType>()
                .Where(type => AxisLabels.ContainsKey(new(deviceControllerType, rhythmType, type))).Cast<object>())
            .Concat(Enum.GetValues<StandardButtonType>()
                .Where(type => ButtonLabels.ContainsKey(new(deviceControllerType, rhythmType, type))).Cast<object>());
    }
}