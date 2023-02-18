using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class WiiCombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<WiiInputType, StandardButtonType> Buttons = new()
    {
        {WiiInputType.ClassicA, StandardButtonType.A},
        {WiiInputType.ClassicB, StandardButtonType.B},
        {WiiInputType.ClassicX, StandardButtonType.X},
        {WiiInputType.ClassicY, StandardButtonType.Y},
        {WiiInputType.ClassicLt, StandardButtonType.L2},
        {WiiInputType.ClassicRt, StandardButtonType.R2},
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
        {WiiInputType.DrumKickPedal, StandardButtonType.RightShoulder}
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

    private static readonly Dictionary<WiiInputType, RBButtonType> TapRb = new()
    {
        {WiiInputType.GuitarTapGreen, RBButtonType.UpperGreen},
        {WiiInputType.GuitarTapRed, RBButtonType.UpperRed},
        {WiiInputType.GuitarTapYellow, RBButtonType.UpperYellow},
        {WiiInputType.GuitarTapBlue, RBButtonType.UpperBlue},
        {WiiInputType.GuitarTapOrange, RBButtonType.UpperOrange}
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
        {WiiInputType.GuitarWhammy, StandardAxisType.RightStickX}
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
        {WiiInputType.DrumGreenPressure, DrumAxisType.Green},
        {WiiInputType.DrumRedPressure, DrumAxisType.Red},
        {WiiInputType.DrumYellowPressure, DrumAxisType.Red},
        {WiiInputType.DrumBluePressure, DrumAxisType.Blue},
        {WiiInputType.DrumOrangePressure, DrumAxisType.Orange},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisRb = new()
    {
        {WiiInputType.DrumGreenPressure, DrumAxisType.Green},
        {WiiInputType.DrumRedPressure, DrumAxisType.Red},
        {WiiInputType.DrumYellowPressure, DrumAxisType.Red},
        {WiiInputType.DrumBluePressure, DrumAxisType.Blue},
        {WiiInputType.DrumOrangePressure, DrumAxisType.Green},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick}
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> AxisAcceleration = new()
    {
        {WiiInputType.NunchukRotationRoll, StandardAxisType.RightStickX},
        {WiiInputType.NunchukRotationPitch, StandardAxisType.RightStickY}
    };

    private readonly Microcontroller _microcontroller;

    private WiiControllerType? _detectedType;

    public WiiCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, microcontroller, WiiInput.WiiTwiType,
        WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        _microcontroller = microcontroller;
        Outputs.Clear();
        if (outputs != null)
            Outputs.AddRange(outputs);
        else
            CreateDefaults();

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(this.WhenAnyValue(x => x.DetectedType).Select(CreateFilter)).Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(this.WhenAnyValue(x => x.DetectedType).Select(CreateFilter)).Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    public string DetectedType => _detectedType?.ToString() ?? "None";

    private static Func<Output, bool> CreateFilter(string s)
    {
        return output =>
            s == "None" || (output.Input is WiiInput wiiInput && wiiInput.WiiControllerType.ToString() == s);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "Wii Extension Inputs";
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
            Outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), 10,
                pair.Value));

        foreach (var pair in Axis)
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), -30000, 30000, 10, pair.Value));

        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Black, Colors.Black, null, 10, StandardButtonType.Left));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyHigh, 32),
        //     Colors.Black, Colors.Black, null, 10, StandardButtonType.Right));
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Black, Colors.Black, null, 10, StandardButtonType.Up));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Black, Colors.Black, null, 10, StandardButtonType.Down));

        Outputs.Add(new ControllerAxis(Model,
            new WiiInput(WiiInputType.GuitarTapBar, Model, _microcontroller, Sda, Scl, true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
            StandardAxisType.RightStickY));
        foreach (var pair in AxisAcceleration)
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, pair.Value));
        foreach (var pair in Tap)
            Outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), 5, pair.Value));
        UpdateBindings();
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedWiiCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }


    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType);
        if (!wiiControllerType.Any())
        {
            this.RaisePropertyChanging(nameof(DetectedType));
            _detectedType = null;
            this.RaisePropertyChanged(nameof(DetectedType));
            return;
        }

        var type = BitConverter.ToUInt16(wiiControllerType);
        var newType = ControllerTypeById.GetValueOrDefault(type);
        if (newType == _detectedType) return;
        this.RaisePropertyChanging(nameof(DetectedType));
        _detectedType = newType;
        this.RaisePropertyChanged(nameof(DetectedType));
    }

    private bool OutputValid(Output output)
    {
        Console.WriteLine(DetectedType);
        if (_detectedType != null)
            return output.Input is WiiInput wiiInput &&
                   wiiInput.WiiControllerType == _detectedType;

        return true;
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
                    Outputs.Add(new DrumAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                        Colors.Black,
                        Colors.Black, Array.Empty<byte>(), -30000, 30000, 10, 64, 10, pair.Value));
            }
            else
            {
                // We already have drum inputs mapped, but need to handle swapping between GH and RB 
                var first = (Outputs.Items.First(s => s.Input is WiiInput
                {
                    Input: WiiInputType.DrumOrangePressure
                }) as DrumAxis)!;
                Outputs.Remove(first);
                // Rb maps orange to green, while gh maps orange to orange
                if (Model.RhythmType == RhythmType.GuitarHero)
                    Outputs.Add(new DrumAxis(Model,
                        new WiiInput(WiiInputType.DrumOrangePressure, Model, _microcontroller, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64,
                        10,
                        DrumAxisType.Orange));
                else
                    Outputs.Add(new DrumAxis(Model,
                        new WiiInput(WiiInputType.DrumOrangePressure, Model, _microcontroller, Sda, Scl, true),
                        first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64,
                        10,
                        DrumAxisType.Green));
            }
        }
        else
        {
            // Remove all drum inputs if we aren't in Drum emulation mode
            Outputs.RemoveMany(Outputs.Items.Where(s => s is DrumAxis));
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
                    GuitarAxisType.Whammy)));
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
                    StandardAxisType.RightStickX)));
            }
        }

        // Map Tap bar to Upper frets on RB guitars, and standard frets on anything else
        if (Model.DeviceType is DeviceControllerType.Guitar && Model.RhythmType is RhythmType.RockBand)
        {
            if (!Outputs.Items.Any(s => s is RbButton))
            {
                var items = Outputs.Items.Where(s => s is ControllerButton
                {
                    Input: WiiInput
                    {
                        Input: WiiInputType.GuitarTapGreen or WiiInputType.GuitarTapRed
                        or WiiInputType.GuitarTapYellow or WiiInputType.GuitarTapBlue
                        or WiiInputType.GuitarTapOrange
                    }
                }).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Cast<RbButton>().Select(item => new RbButton(Model, item.Input,
                    item.LedOn,
                    item.LedOff, item.LedIndices.ToArray(), item.Debounce,
                    TapRb[item.WiiInputType])));
            }
        }
        else
        {
            var items2 = Outputs.Items.Where(s => s is RbButton).ToList();
            if (items2.Any())
            {
                Outputs.RemoveMany(items2);
                Outputs.AddRange(items2.Cast<RbButton>().Select(item => new ControllerButton(Model, item.Input,
                    item.LedOn,
                    item.LedOff, item.LedIndices.ToArray(), item.Debounce,
                    Tap[item.WiiInputType])));
            }
        }

        // Map Slider on GH guitars to Slider, and to RightStickY on anything else
        if (Model.DeviceType is DeviceControllerType.Guitar && Model.RhythmType is RhythmType.GuitarHero)
        {
            if (!Outputs.Items.Any(s => s is GuitarAxis {Type: GuitarAxisType.Slider}))
            {
                var items = Outputs.Items.Where(s => s is ControllerAxis {Type: StandardAxisType.RightStickY}).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Cast<ControllerAxis>().Select(item => new GuitarAxis(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    GuitarAxisType.Slider)));
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
                    StandardAxisType.RightStickY)));
            }
        }

        // Map all DJ Hero axis and buttons
        var currentAxisDj = Outputs.Items.OfType<DjAxis>();
        var currentAxisStandard = Outputs.Items.OfType<ControllerAxis>().ToList();
        var currentButtonDj = Outputs.Items.OfType<DjButton>();
        var currentButtonStandard = Outputs.Items.OfType<ControllerButton>().ToList();
        if (Model.DeviceType is DeviceControllerType.Turntable)
        {
            foreach (var (djInputType, wiiInputType) in DjToWiiButton)
            {
                var items = currentButtonStandard.Where(s => s.Input is WiiInput wii && wii.Input == wiiInputType)
                    .ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Select(item => new DjButton(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Debounce, djInputType)));
            }

            foreach (var (dj, standard) in DjToStandard)
            {
                var items = currentAxisStandard.Where(s => s.Type == standard).ToList();
                Outputs.RemoveMany(items);
                Outputs.AddRange(items.Select(item => new DjAxis(Model, item.Input,
                    item.LedOn, item.LedOff, item.LedIndices.ToArray(), item.Min, item.Max, item.DeadZone,
                    dj)));
            }
        }
        else
        {
            foreach (var djButton in currentButtonDj)
            {
                Outputs.Remove(djButton);
                Outputs.Add(new ControllerButton(Model, djButton.Input,
                    djButton.LedOn, djButton.LedOff, djButton.LedIndices.ToArray(), djButton.Debounce,
                    Buttons[DjToWiiButton[djButton.Type]]));
            }

            foreach (var djAxis in currentAxisDj)
            {
                Outputs.Remove(djAxis);
                Outputs.Add(new ControllerAxis(Model, djAxis.Input,
                    djAxis.LedOn, djAxis.LedOff, djAxis.LedIndices.ToArray(), djAxis.Min, djAxis.Max, djAxis.DeadZone,
                    DjToStandard[djAxis.Type]));
            }
        }
    }
}