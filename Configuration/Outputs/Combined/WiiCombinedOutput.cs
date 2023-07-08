using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class WiiCombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<WiiInputType, StandardButtonType> Buttons = new()
    {
        {WiiInputType.ClassicA, StandardButtonType.A},
        {WiiInputType.ClassicB, StandardButtonType.B},
        {WiiInputType.ClassicX, StandardButtonType.X},
        {WiiInputType.ClassicY, StandardButtonType.Y},
        {WiiInputType.ClassicZl, StandardButtonType.LeftShoulder},
        {WiiInputType.ClassicZr, StandardButtonType.RightShoulder},
        {WiiInputType.ClassicMinus, StandardButtonType.Back},
        {WiiInputType.ClassicPlus, StandardButtonType.Start},
        {WiiInputType.ClassicHome, StandardButtonType.Guide},
        {WiiInputType.ClassicDPadDown, StandardButtonType.DpadDown},
        {WiiInputType.ClassicDPadUp, StandardButtonType.DpadUp},
        {WiiInputType.ClassicDPadLeft, StandardButtonType.DpadLeft},
        {WiiInputType.ClassicDPadRight, StandardButtonType.DpadRight},
        {WiiInputType.DjHeroLeftGreen, StandardButtonType.A},
        {WiiInputType.DjHeroLeftRed, StandardButtonType.B},
        {WiiInputType.DjHeroLeftBlue, StandardButtonType.X},
        {WiiInputType.DjHeroRightGreen, StandardButtonType.A},
        {WiiInputType.DjHeroRightRed, StandardButtonType.B},
        {WiiInputType.DjHeroRightBlue, StandardButtonType.X},
        {WiiInputType.DjHeroEuphoria, StandardButtonType.Y},
        {WiiInputType.NunchukC, StandardButtonType.A},
        {WiiInputType.NunchukZ, StandardButtonType.B},
        {WiiInputType.GuitarGreen, StandardButtonType.A},
        {WiiInputType.GuitarRed, StandardButtonType.B},
        {WiiInputType.GuitarYellow, StandardButtonType.Y},
        {WiiInputType.GuitarBlue, StandardButtonType.X},
        {WiiInputType.GuitarOrange, StandardButtonType.LeftShoulder},
        {WiiInputType.GuitarTapAll, StandardButtonType.A},
        {WiiInputType.GuitarStrumDown, StandardButtonType.DpadDown},
        {WiiInputType.GuitarStrumUp, StandardButtonType.DpadUp},
        {WiiInputType.GuitarMinus, StandardButtonType.Back},
        {WiiInputType.GuitarPlus, StandardButtonType.Start},
        {WiiInputType.UDrawPenClick, StandardButtonType.A},
        {WiiInputType.UDrawPenButton1, StandardButtonType.X},
        {WiiInputType.UDrawPenButton2, StandardButtonType.Y},
        {WiiInputType.TaTaConLeftDrumCenter, StandardButtonType.A},
        {WiiInputType.TaTaConLeftDrumRim, StandardButtonType.B},
        {WiiInputType.TaTaConRightDrumCenter, StandardButtonType.X},
        {WiiInputType.TaTaConRightDrumRim, StandardButtonType.Y},
        {WiiInputType.DrumGreen, StandardButtonType.A},
        {WiiInputType.DrumRed, StandardButtonType.B},
        {WiiInputType.DrumYellow, StandardButtonType.Y},
        {WiiInputType.DrumBlue, StandardButtonType.X},
        {WiiInputType.DrumOrange, StandardButtonType.LeftShoulder},
        {WiiInputType.DrumKickPedal, StandardButtonType.RightShoulder},
        {WiiInputType.DrumMinus, StandardButtonType.Back},
        {WiiInputType.DrumPlus, StandardButtonType.Start},
    };


    public static readonly Dictionary<int, WiiControllerType> ControllerTypeById = new()
    {
        {0x0000, WiiControllerType.Nunchuk},
        {0x0001, WiiControllerType.ClassicController},
        {0x0101, WiiControllerType.ClassicControllerPro},
        {0x0301, WiiControllerType.ClassicControllerPro},
        {0xFF12, WiiControllerType.UDraw},
        {0xFF13, WiiControllerType.Drawsome},
        {0x0003, WiiControllerType.Guitar},
        {0x0103, WiiControllerType.Drum},
        {0x0303, WiiControllerType.Dj},
        {0x0011, WiiControllerType.Taiko},
        {0x0005, WiiControllerType.MotionPlus}
    };

    private static readonly Dictionary<WiiInputType, StandardButtonType> Tap = new()
    {
        {WiiInputType.GuitarTapGreen, StandardButtonType.A},
        {WiiInputType.GuitarTapRed, StandardButtonType.B},
        {WiiInputType.GuitarTapYellow, StandardButtonType.Y},
        {WiiInputType.GuitarTapBlue, StandardButtonType.X},
        {WiiInputType.GuitarTapOrange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<WiiInputType, InstrumentButtonType> TapRb = new()
    {
        {WiiInputType.GuitarTapGreen, InstrumentButtonType.SoloGreen},
        {WiiInputType.GuitarTapRed, InstrumentButtonType.SoloRed},
        {WiiInputType.GuitarTapYellow, InstrumentButtonType.SoloYellow},
        {WiiInputType.GuitarTapBlue, InstrumentButtonType.SoloBlue},
        {WiiInputType.GuitarTapOrange, InstrumentButtonType.SoloOrange}
    };

    private static readonly Dictionary<WiiInputType, StandardAxisType> Axis = new()
    {
        {WiiInputType.ClassicLeftStickX, StandardAxisType.LeftStickX},
        {WiiInputType.ClassicLeftStickY, StandardAxisType.LeftStickY},
        {WiiInputType.ClassicRightStickX, StandardAxisType.RightStickX},
        {WiiInputType.ClassicRightStickY, StandardAxisType.RightStickY},
        {WiiInputType.ClassicLeftTrigger, StandardAxisType.LeftTrigger},
        {WiiInputType.ClassicRightTrigger, StandardAxisType.RightTrigger},
        {WiiInputType.DjCrossfadeSlider, StandardAxisType.RightStickY},
        {WiiInputType.DjEffectDial, StandardAxisType.RightStickX},
        {WiiInputType.DjTurntableLeft, StandardAxisType.LeftStickX},
        {WiiInputType.DjTurntableRight, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenX, StandardAxisType.LeftStickX},
        {WiiInputType.UDrawPenY, StandardAxisType.LeftStickY},
        {WiiInputType.UDrawPenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.DrawsomePenX, StandardAxisType.LeftStickX},
        {WiiInputType.DrawsomePenY, StandardAxisType.LeftStickY},
        {WiiInputType.DrawsomePenPressure, StandardAxisType.LeftTrigger},
        {WiiInputType.NunchukStickX, StandardAxisType.LeftStickX},
        {WiiInputType.NunchukStickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarJoystickX, StandardAxisType.LeftStickX},
        {WiiInputType.GuitarJoystickY, StandardAxisType.LeftStickY},
        {WiiInputType.DrumJoystickX, StandardAxisType.LeftStickX},
        {WiiInputType.DrumJoystickY, StandardAxisType.LeftStickY},
        {WiiInputType.GuitarWhammy, StandardAxisType.RightStickX}
    };

    public static readonly List<WiiInputType> UIntInputs = new()
    {
        WiiInputType.ClassicLeftTrigger,
        WiiInputType.ClassicRightTrigger,
        WiiInputType.DrumGreenPressure,
        WiiInputType.DrumRedPressure,
        WiiInputType.DrumYellowPressure,
        WiiInputType.DrumBluePressure,
        WiiInputType.DrumOrangePressure,
        WiiInputType.DrumKickPedalPressure,
        WiiInputType.GuitarWhammy,
        WiiInputType.GuitarTapBar,
        WiiInputType.UDrawPenPressure,
        WiiInputType.DrawsomePenPressure,
    };

    private static readonly Dictionary<DjAxisType, StandardAxisType> DjToStandard = new()
    {
        {DjAxisType.Crossfader, StandardAxisType.RightStickY},
        {DjAxisType.EffectsKnob, StandardAxisType.RightStickX},
        {DjAxisType.LeftTableVelocity, StandardAxisType.LeftStickX},
        {DjAxisType.RightTableVelocity, StandardAxisType.LeftStickY}
    };

    private static readonly Dictionary<DjInputType, WiiInputType> DjToWiiButton = new()
    {
        {DjInputType.LeftGreen, WiiInputType.DjHeroLeftGreen},
        {DjInputType.LeftRed, WiiInputType.DjHeroLeftRed},
        {DjInputType.LeftBlue, WiiInputType.DjHeroLeftBlue},
        {DjInputType.RightGreen, WiiInputType.DjHeroRightGreen},
        {DjInputType.RightRed, WiiInputType.DjHeroRightRed},
        {DjInputType.RightBlue, WiiInputType.DjHeroRightBlue}
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisGh = new()
    {
        {WiiInputType.DrumGreen, DrumAxisType.Green},
        {WiiInputType.DrumRed, DrumAxisType.Red},
        {WiiInputType.DrumYellow, DrumAxisType.Yellow},
        {WiiInputType.DrumBlue, DrumAxisType.Blue},
        {WiiInputType.DrumOrange, DrumAxisType.Orange},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisRb = new()
    {
        {WiiInputType.DrumGreen, DrumAxisType.Green},
        {WiiInputType.DrumRed, DrumAxisType.Red},
        {WiiInputType.DrumYellow, DrumAxisType.Yellow},
        {WiiInputType.DrumBlue, DrumAxisType.Blue},
        {WiiInputType.DrumOrange, DrumAxisType.Green},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> AxisAcceleration = new()
    {
        {WiiInputType.NunchukRotationRoll, StandardAxisType.RightStickX},
        {WiiInputType.NunchukRotationPitch, StandardAxisType.RightStickY}
    };

    public WiiCombinedOutput(ConfigViewModel model, int sda = -1, int scl = -1) : base(model, WiiInput.WiiTwiType,
        WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        Outputs.Clear();
        this.WhenAnyValue(x => x.DetectedType).Select(s => s is WiiControllerType.Guitar)
            .ToPropertyEx(this, x => x.IsGuitar);
        Outputs.Connect().Filter(x => x is OutputAxis)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType).Select(CreateFilter))
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton or JoystickToDpad)
            .AutoRefresh(s => s.LocalisedName)
            .Filter(s => s.LocalisedName.Any())
            .Filter(this.WhenAnyValue(x => x.ControllerFound, x => x.DetectedType).Select(CreateFilter))
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsGuitar { get; }

    [Reactive] public WiiControllerType DetectedType { get; set; }

    [Reactive] public bool ControllerFound { get; set; }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    private static Func<Output, bool> CreateFilter((bool controllerFound, WiiControllerType controllerType) tuple)
    {
        return output => !tuple.controllerFound || output is JoystickToDpad || output.Input is WiiInput wiiInput &&
            wiiInput.WiiControllerType == tuple.controllerType;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Wii Extension Inputs";
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
        {
            var output = new ControllerButton(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), 10,
                pair.Value, true);
            if (pair.Key == WiiInputType.GuitarTapAll)
            {
                if (Model.DeviceType != DeviceControllerType.Guitar)
                {
                    continue;
                }

                output.Enabled = false;
            }

            Outputs.Add(output);
        }

        foreach (var pair in Axis)
        {
            if (UIntInputs.Contains(pair.Key))
            {
                Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 0, ushort.MaxValue, 8000, pair.Value, true));
            }
            else
            {
                Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), -30000, 30000, 4000, pair.Value, true));
            }
        }

        Outputs.Add(new ControllerAxis(Model,
            new WiiInput(WiiInputType.GuitarTapBar, Model, Sda, Scl, true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
            StandardAxisType.RightStickY, true));
        foreach (var pair in AxisAcceleration)
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, pair.Value, true));
        var dpad = new JoystickToDpad(Model, short.MaxValue / 2, true)
        {
            Enabled = false
        };
        Outputs.Add(dpad);
        UpdateBindings();
    }

    public override IEnumerable<Output> ValidOutputs()
    {
        var outputs = new List<Output>(base.ValidOutputs());
        var joyToDpad = outputs.FirstOrDefault(s => s is JoystickToDpad);
        if (joyToDpad?.Enabled == true)
        {
            outputs.Remove(joyToDpad);
            outputs.Add(joyToDpad.ValidOutputs());
        }

        var tapAnalog =
            outputs.FirstOrDefault(s => s is {Enabled: true, Input: WiiInput {Input: WiiInputType.GuitarTapBar}});
        var tapFrets =
            outputs.FirstOrDefault(s => s is {Enabled: true, Input: WiiInput {Input: WiiInputType.GuitarTapAll}});
        if (tapAnalog == null && tapFrets == null) return outputs;
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceType is DeviceControllerType.Guitar &&
            Model.RhythmType is RhythmType.RockBand)
        {
            outputs.AddRange(TapRb.Select(pair => new GuitarButton(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 5, pair.Value, true)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        if (Model.DeviceType == DeviceControllerType.Guitar)
        {
            outputs.AddRange(Tap.Select(pair => new ControllerButton(Model,
                new WiiInput(pair.Key, Model, Sda, Scl, true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 5, pair.Value, true)));
        }

        outputs.Remove(tapFrets);

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedWiiCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] usbHostRaw, byte[] bluetoothRaw, byte[] usbHostInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
        if (!wiiControllerType.Any())
        {
            ControllerFound = false;
            return;
        }

        ControllerFound = true;

        var type = BitConverter.ToUInt16(wiiControllerType);
        var newType = ControllerTypeById.GetValueOrDefault(type);

        DetectedType = newType;
    }

    public override void UpdateBindings()
    {
        // Drum Specific mappings
        if (Model.DeviceType == DeviceControllerType.Drum)
        {
            // Drum Inputs to Drum Axis
            if (!Outputs.Items.Any(s => s is DrumAxis))
            {
                foreach (var pair in Model.RhythmType == RhythmType.GuitarHero ? DrumAxisGh : DrumAxisRb)
                    Outputs.Add(new DrumAxis(Model, new WiiInput(pair.Key, Model, Sda, Scl, true),
                        Colors.Black,
                        Colors.Black, Array.Empty<byte>(), -30000, 30000, 10, 64, 10, pair.Value, true));
            }
            else
            {
                // We already have drum inputs mapped, but need to handle swapping between GH and RB 
                var first = Outputs.Items.OfType<DrumAxis>().First(s => s.Input is WiiInput
                {
                    Input: WiiInputType.DrumOrange
                });
                Outputs.Remove(first);
                // Rb maps orange to green, while gh maps orange to orange
                if (Model.RhythmType == RhythmType.GuitarHero)
                    Outputs.Add(new DrumAxis(Model,
                        new WiiInput(WiiInputType.DrumOrange, Model, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64,
                        10,
                        DrumAxisType.Orange, true));
                else
                    Outputs.Add(new DrumAxis(Model,
                        new WiiInput(WiiInputType.DrumOrange, Model, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64,
                        10,
                        DrumAxisType.Green, true));
            }
        }
        else
        {
            // Remove all drum inputs if we aren't in Drum emulation mode
            Outputs.RemoveMany(Outputs.Items.Where(s => s is DrumAxis));
        }

        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: WiiInput {Input: WiiInputType.GuitarTapAll}});

        if (Model.DeviceType == DeviceControllerType.Guitar)
        {
            if (tapFrets == null)
            {
                Outputs.Add(new ControllerButton(Model, new WiiInput(WiiInputType.GuitarTapAll, Model, Sda, Scl, true),
                    Colors.Black,
                    Colors.Black, Array.Empty<byte>(), 10,
                    StandardButtonType.A, true)
                {
                    Enabled = false
                });
            }
        }
        else if (Model.DeviceType != DeviceControllerType.Guitar)
        {
            if (tapFrets != null)
            {
                Outputs.Remove(tapFrets);
            }
        }

        // Map the Whammy axis to right stick x on anything that isnt a guitar, and whammy on a guitar
        if (Model.DeviceType is DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar)
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}))
            {
                var items = Outputs.Items.Where(s => s is ControllerAxis {Type: StandardAxisType.RightStickX}).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Cast<ControllerAxis>().Select(item => new GuitarAxis(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    GuitarAxisType.Whammy, true)));
            }
        }
        else
        {
            var items2 = Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Whammy}).ToList();
            if (items2.Any())
            {
                Outputs.RemoveMany(items2);
                Outputs.AddRange(items2.Cast<GuitarAxis>().Select(item => new ControllerAxis(Model, item.Input,
                    item.LedOn,
                    item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    StandardAxisType.RightStickX, true)));
            }
        }

        // Map Slider on guitars to Slider, and to RightStickY on anything else
        if (Model.DeviceType is DeviceControllerType.Guitar)
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Slider}))
            {
                var items = Outputs.Items.Where(s => s is ControllerAxis {Type: StandardAxisType.RightStickY}).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Cast<ControllerAxis>().Select(item => new GuitarAxis(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    GuitarAxisType.Slider, true)));
            }
        }
        else
        {
            var items2 = Outputs.Items.Where(s => s is GuitarAxis {Type: GuitarAxisType.Slider}).ToList();
            if (items2.Any())
            {
                Outputs.RemoveMany(items2);
                Outputs.AddRange(items2.Cast<GuitarAxis>().Select(item => new ControllerAxis(Model, item.Input,
                    item.LedOn,
                    item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    StandardAxisType.RightStickY, true)));
            }
        }

        InstrumentButtonTypeExtensions.ConvertBindings(Outputs, Model, true);

        // Map all DJ Hero axis and buttons
        if (Model.DeviceType is DeviceControllerType.Turntable)
        {
            var currentAxisStandard = Outputs.Items.OfType<ControllerAxis>().ToList();
            var currentButtonStandard = Outputs.Items.OfType<ControllerButton>().ToList();
            foreach (var (djInputType, wiiInputType) in DjToWiiButton)
            {
                var items = currentButtonStandard.Where(s => s.Input is WiiInput wii && wii.Input == wiiInputType)
                    .ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Select(item => new DjButton(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Debounce, djInputType, true)));
            }

            foreach (var (dj, standard) in DjToStandard)
            {
                var items = currentAxisStandard.Where(s => s.Type == standard).ToList();
                Outputs.RemoveMany(items);
                if (dj is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity or DjAxisType.EffectsKnob)
                {
                    Outputs.AddRange(items.Select(item => new DjAxis(Model, item.Input,
                        item.LedOn, item.LedOff, item.LedIndices.ToArray(), 1,
                        dj, true)));
                }
                else
                {
                    Outputs.AddRange(items.Select(item => new DjAxis(Model, item.Input,
                        item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                        dj, true)));
                }
            }
        }
        else
        {
            var currentAxisDj = Outputs.Items.OfType<DjAxis>();
            var currentButtonDj = Outputs.Items.OfType<DjButton>();
            foreach (var djButton in currentButtonDj)
            {
                Outputs.Remove(djButton);
                Outputs.Add(new ControllerButton(Model, djButton.Input,
                    djButton.LedOn, djButton.LedOff, djButton.LedIndices.ToArray(), djButton.Debounce,
                    Buttons[DjToWiiButton[djButton.Type]], true));
            }

            foreach (var djAxis in currentAxisDj)
            {
                Outputs.Remove(djAxis);
                Outputs.Add(new ControllerAxis(Model, djAxis.Input,
                    djAxis.LedOn, djAxis.LedOff, djAxis.LedIndices.ToArray(), djAxis.Min, djAxis.Max, djAxis.DeadZone,
                    DjToStandard[djAxis.Type], true));
            }
        }
    }

    public override object GetOutputType()
    {
        return SimpleType.WiiInputSimple;
    }
}