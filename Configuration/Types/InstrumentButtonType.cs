using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum InstrumentButtonType
{
    [Description("Solo Green Fret")] SoloGreen,
    [Description("Solo Red Fret")] SoloRed,
    [Description("Solo Yellow Fret")] SoloYellow,
    [Description("Solo Blue Fret")] SoloBlue,
    [Description("Solo Orange Fret")] SoloOrange,
    [Description("Green Fret")] Green,
    [Description("Red Fret")] Red,
    [Description("Yellow Fret")] Yellow,
    [Description("Blue Fret")] Blue,
    [Description("Orange Fret")] Orange,
    Black1,
    Black2,
    Black3,
    White1,
    White2,
    White3,
    [Description("Strum Up")]
    StrumUp,
    [Description("Strum Down")]
    StrumDown,
}

public static class InstrumentButtonTypeExtensions
{
    
    public static readonly Dictionary<InstrumentButtonType, InstrumentButtonType> GuitarToLive = new()
    {
        {InstrumentButtonType.Green, InstrumentButtonType.Black1},
        {InstrumentButtonType.Red, InstrumentButtonType.Black2},
        {InstrumentButtonType.Yellow, InstrumentButtonType.Black3},
        {InstrumentButtonType.Blue, InstrumentButtonType.White1},
        {InstrumentButtonType.Orange, InstrumentButtonType.White2},
    };
    
    public static readonly Dictionary<InstrumentButtonType, InstrumentButtonType> LiveToGuitar = new()
    {
        {InstrumentButtonType.Black1, InstrumentButtonType.Green},
        {InstrumentButtonType.Black2, InstrumentButtonType.Red},
        {InstrumentButtonType.Black3, InstrumentButtonType.Yellow},
        {InstrumentButtonType.White1, InstrumentButtonType.Blue},
        {InstrumentButtonType.White2, InstrumentButtonType.Orange},
    };



    public static readonly Dictionary<StandardButtonType, InstrumentButtonType> GuitarMappings = new()
    {
        {StandardButtonType.A, InstrumentButtonType.Green},
        {StandardButtonType.B, InstrumentButtonType.Red},
        {StandardButtonType.Y, InstrumentButtonType.Yellow},
        {StandardButtonType.X, InstrumentButtonType.Blue},
        {StandardButtonType.LeftShoulder, InstrumentButtonType.Orange},
        {StandardButtonType.DpadUp, InstrumentButtonType.StrumUp},
        {StandardButtonType.DpadDown, InstrumentButtonType.StrumDown},
    };

    public static readonly Dictionary<StandardButtonType, InstrumentButtonType> LiveGuitarMappings = new()
    {
        {StandardButtonType.A, InstrumentButtonType.Black1},
        {StandardButtonType.B, InstrumentButtonType.Black2},
        {StandardButtonType.Y, InstrumentButtonType.Black3},
        {StandardButtonType.X, InstrumentButtonType.White1},
        {StandardButtonType.LeftShoulder, InstrumentButtonType.White2},
        {StandardButtonType.RightShoulder, InstrumentButtonType.White3},
        {StandardButtonType.DpadUp, InstrumentButtonType.StrumUp},
        {StandardButtonType.DpadDown, InstrumentButtonType.StrumDown},
    };
    public static readonly Dictionary<InstrumentButtonType, StandardButtonType> GuitarToStandard = new()
    {
        {InstrumentButtonType.Black1, StandardButtonType.A},
        {InstrumentButtonType.Black2, StandardButtonType.B},
        {InstrumentButtonType.Black3, StandardButtonType.Y},
        {InstrumentButtonType.White1, StandardButtonType.X},
        {InstrumentButtonType.White2, StandardButtonType.LeftShoulder},
        {InstrumentButtonType.White3, StandardButtonType.RightShoulder},
        {InstrumentButtonType.StrumUp, StandardButtonType.DpadUp},
        {InstrumentButtonType.StrumDown, StandardButtonType.DpadDown},
        {InstrumentButtonType.Green, StandardButtonType.A},
        {InstrumentButtonType.Red, StandardButtonType.B},
        {InstrumentButtonType.Yellow, StandardButtonType.Y},
        {InstrumentButtonType.Blue, StandardButtonType.X},
        {InstrumentButtonType.Orange, StandardButtonType.LeftShoulder},
    };
    private static readonly InstrumentButtonType[] GuitarButtons =
    {
        InstrumentButtonType.Green,
        InstrumentButtonType.Red,
        InstrumentButtonType.Yellow,
        InstrumentButtonType.Blue,
        InstrumentButtonType.Orange,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    private static readonly InstrumentButtonType[] RbButtons =
    {
        InstrumentButtonType.Green,
        InstrumentButtonType.Red,
        InstrumentButtonType.Yellow,
        InstrumentButtonType.Blue,
        InstrumentButtonType.Orange,
        InstrumentButtonType.SoloGreen,
        InstrumentButtonType.SoloRed,
        InstrumentButtonType.SoloYellow,
        InstrumentButtonType.SoloBlue,
        InstrumentButtonType.SoloOrange,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    private static readonly InstrumentButtonType[] GhlButtons =
    {
        InstrumentButtonType.Black1,
        InstrumentButtonType.Black2,
        InstrumentButtonType.Black3,
        InstrumentButtonType.White1,
        InstrumentButtonType.White2,
        InstrumentButtonType.White3,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    public static IEnumerable<InstrumentButtonType> GetButtons(DeviceControllerType deviceControllerType,
        RhythmType type)
    {
        return deviceControllerType switch
        {
            DeviceControllerType.Guitar when type is RhythmType.GuitarHero => GuitarButtons,
            DeviceControllerType.Guitar when type is RhythmType.RockBand => RbButtons,
            DeviceControllerType.LiveGuitar => GhlButtons,
            _ => Enumerable.Empty<InstrumentButtonType>()
        };
    }
    
    public static void ConvertBindings(SourceList<Output> outputs, ConfigViewModel model)
    {
        switch (model.DeviceType)
        {
            case DeviceControllerType.Guitar:
            {
                foreach (var output in outputs.Items)
                {
                    if (output is GuitarButton guitarButton)
                    {
                        if (!LiveToGuitar.ContainsKey(guitarButton.Type)) continue;
                        outputs.Remove(output);
                        outputs.Add(new GuitarButton(model, output.Input, output.LedOn, output.LedOff,
                            output.LedIndices.ToArray(), guitarButton.Debounce, LiveToGuitar[guitarButton.Type]));
                    }
                    if (output is not ControllerButton button) continue;
                    if (!GuitarMappings.ContainsKey(button.Type)) continue;
                    outputs.Remove(output);
                    outputs.Add(new GuitarButton(model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), button.Debounce, GuitarMappings[button.Type]));
                }

                break;
            }
            case DeviceControllerType.LiveGuitar:
            {
                foreach (var output in outputs.Items)
                {
                    if (output is GuitarButton guitarButton)
                    {
                        if (!GuitarToLive.ContainsKey(guitarButton.Type)) continue;
                        outputs.Remove(output);
                        outputs.Add(new GuitarButton(model, output.Input, output.LedOn, output.LedOff,
                            output.LedIndices.ToArray(), guitarButton.Debounce, GuitarToLive[guitarButton.Type]));
                    }
                    if (output is not ControllerButton button) continue;
                    if (!LiveGuitarMappings.ContainsKey(button.Type)) continue;
                    outputs.Remove(output);
                    outputs.Add(new GuitarButton(model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), button.Debounce, LiveGuitarMappings[button.Type]));
                }

                break;
            }
            default:
            {
                foreach (var output in outputs.Items)
                {
                    if (output is not GuitarButton guitarButton) continue;
                    outputs.Remove(output);
                    outputs.Add(new ControllerButton(model, output.Input, output.LedOn, output.LedOff,
                        output.LedIndices.ToArray(), guitarButton.Debounce, GuitarToStandard[guitarButton.Type]));
                }

                break;
            }
        }
    }
}