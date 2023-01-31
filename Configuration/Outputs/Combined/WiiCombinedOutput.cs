using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using DynamicData;
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
        {WiiInputType.ClassicLt, StandardButtonType.Lt},
        {WiiInputType.ClassicRt, StandardButtonType.Rt},
        {WiiInputType.ClassicZl, StandardButtonType.Lb},
        {WiiInputType.ClassicZr, StandardButtonType.Rb},
        {WiiInputType.ClassicMinus, StandardButtonType.Select},
        {WiiInputType.ClassicPlus, StandardButtonType.Start},
        {WiiInputType.ClassicHome, StandardButtonType.Home},
        {WiiInputType.ClassicDPadDown, StandardButtonType.Down},
        {WiiInputType.ClassicDPadUp, StandardButtonType.Up},
        {WiiInputType.ClassicDPadLeft, StandardButtonType.Left},
        {WiiInputType.ClassicDPadRight, StandardButtonType.Right},
        {WiiInputType.DjHeroLeftGreen, StandardButtonType.A},
        {WiiInputType.DjHeroLeftRed, StandardButtonType.B},
        {WiiInputType.DjHeroLeftBlue, StandardButtonType.X},
        {WiiInputType.DjHeroRightGreen, StandardButtonType.A},
        {WiiInputType.DjHeroRightRed, StandardButtonType.B},
        {WiiInputType.DjHeroRightBlue, StandardButtonType.X},
        {WiiInputType.DjHeroLeftAny, StandardButtonType.Lb},
        {WiiInputType.DjHeroRightAny, StandardButtonType.Rb},
        {WiiInputType.DjHeroEuphoria, StandardButtonType.Y},
        {WiiInputType.NunchukC, StandardButtonType.A},
        {WiiInputType.NunchukZ, StandardButtonType.B},
        {WiiInputType.GuitarGreen, StandardButtonType.A},
        {WiiInputType.GuitarRed, StandardButtonType.B},
        {WiiInputType.GuitarYellow, StandardButtonType.Y},
        {WiiInputType.GuitarBlue, StandardButtonType.X},
        {WiiInputType.GuitarOrange, StandardButtonType.Lb},
        {WiiInputType.GuitarStrumDown, StandardButtonType.Down},
        {WiiInputType.GuitarStrumUp, StandardButtonType.Up},
        {WiiInputType.GuitarMinus, StandardButtonType.Select},
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
        {WiiInputType.DrumOrange, StandardButtonType.Lb},
        {WiiInputType.DrumKickPedal, StandardButtonType.Rb},
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
        {WiiInputType.GuitarTapOrange, StandardButtonType.Lb},
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

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisGh = new()
    {
        {WiiInputType.DrumGreenPressure, DrumAxisType.Green},
        {WiiInputType.DrumRedPressure, DrumAxisType.Red},
        {WiiInputType.DrumYellowPressure, DrumAxisType.Red},
        {WiiInputType.DrumBluePressure, DrumAxisType.Blue},
        {WiiInputType.DrumOrangePressure, DrumAxisType.Orange},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick},
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    private static readonly Dictionary<WiiInputType, DrumAxisType> DrumAxisRb = new()
    {
        {WiiInputType.DrumGreenPressure, DrumAxisType.Green},
        {WiiInputType.DrumRedPressure, DrumAxisType.Red},
        {WiiInputType.DrumYellowPressure, DrumAxisType.Red},
        {WiiInputType.DrumBluePressure, DrumAxisType.Blue},
        {WiiInputType.DrumOrangePressure, DrumAxisType.Green},
        {WiiInputType.DrumKickPedal, DrumAxisType.Kick},
        // {WiiInputType.DrumHiHatPedal, DrumAxisType.Kick2},
    };

    public static readonly Dictionary<WiiInputType, StandardAxisType> AxisAcceleration = new()
    {
        {WiiInputType.NunchukRotationRoll, StandardAxisType.RightStickX},
        {WiiInputType.NunchukRotationPitch, StandardAxisType.RightStickY},
    };

    private readonly Microcontroller _microcontroller;

    public WiiCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, microcontroller, WiiInput.WiiTwiType,
        WiiInput.WiiTwiFreq, "Wii", sda, scl)
    {
        _microcontroller = microcontroller;
        Outputs.Clear();
        if (outputs != null)
        {
            Outputs.AddRange(outputs);
        }
        else
        {
            CreateDefaults();
        }

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Filter(this.WhenAnyValue(x => x.DetectedType).Select(CreateFilter)).Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Filter(this.WhenAnyValue(x => x.DetectedType).Select(CreateFilter)).Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    private static Func<Output, bool> CreateFilter(string s)
    {
        return output => s == "None" || output.Input is WiiInput wiiInput && wiiInput.WiiControllerType.ToString() == s;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
        {
            Outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 10,
                pair.Value));
        }

        foreach (var pair in Axis)
        {
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), -30000, 30000, 10, pair.Value));
        }

        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Left));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickX, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyHigh, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Right));
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Up));
        //
        // _outputs.Add(new ControllerButton(Model,
        //     new AnalogToDigital(new WiiInput(WiiInputType.DjStickY, Model,_microcontroller, Sda, Scl),
        //         AnalogToDigitalType.JoyLow, 32),
        //     Colors.Transparent, Colors.Transparent, null, 10, StandardButtonType.Down));

        Outputs.Add(new ControllerAxis(Model,
            new WiiInput(WiiInputType.GuitarTapBar, Model, _microcontroller, Sda, Scl, true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
            StandardAxisType.RightStickY));
        UpdateBindings();
    }

    public void AddTapBarFrets()
    {
        foreach (var pair in Tap)
        {
            Outputs.Add(new ControllerButton(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }
    }

    public void AddNunchukAcceleration()
    {
        foreach (var pair in AxisAcceleration)
        {
            Outputs.Add(new ControllerAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, pair.Value));
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedWiiCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    private WiiControllerType? _detectedType;

    public string DetectedType => _detectedType?.ToString() ?? "None";


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
        {
            return output.Input is WiiInput wiiInput &&
                   wiiInput.WiiControllerType == _detectedType;
        }

        return true;
    }

    public override void UpdateBindings()
    {
        if (Model.DeviceType == DeviceControllerType.Drum)
        {
            if (!Outputs.Items.Any(s => s is DrumAxis))
            {
                foreach (var pair in Model.RhythmType == RhythmType.GuitarHero ? DrumAxisGh : DrumAxisRb)
                {
                    Outputs.Add(new DrumAxis(Model, new WiiInput(pair.Key, Model, _microcontroller, Sda, Scl, true),
                        Colors.Transparent,
                        Colors.Transparent, Array.Empty<byte>(), -30000, 30000, 10, 64, 10, pair.Value));
                }
            }

            var first = (Outputs.Items.First(s => s.Input is WiiInput
            {
                Input: WiiInputType.DrumOrangePressure
            }) as DrumAxis)!;
            Outputs.Remove(first);
            // Rb maps orange to green, while gh maps orange to orange
            if (Model.RhythmType == RhythmType.GuitarHero)
            {
                Outputs.Add(new DrumAxis(Model,
                    new WiiInput(WiiInputType.DrumOrangePressure, Model, _microcontroller, Sda, Scl, true),
                    first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64, 10,
                    DrumAxisType.Orange));
            }
            else
            {
                Outputs.Add(new DrumAxis(Model,
                    new WiiInput(WiiInputType.DrumOrangePressure, Model, _microcontroller, Sda, Scl, true),
                    first.LedOn, first.LedOff, first.LedIndices.ToArray(), first.Min, first.Max, first.DeadZone, 64, 10,
                    DrumAxisType.Green));
            }
        }
        else
        {
            Outputs.RemoveMany(Outputs.Items.Where(s => s is DrumAxis));
            // Flip guitar bindings for live guitar
            Outputs.RemoveMany(Outputs.Items.Where(s => s.WiiInputType == WiiInputType.GuitarWhammy));
            switch (Model.DeviceType)
            {
                case DeviceControllerType.LiveGuitar:
                    Outputs.Add(new ControllerAxis(Model,
                        new WiiInput(WiiInputType.GuitarWhammy, Model, _microcontroller, Sda, Scl, combined: true),
                        Colors.Transparent,
                        Colors.Transparent, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, StandardAxisType.RightStickY));
                    break;
                default:
                    Outputs.Add(new ControllerAxis(Model,
                        new WiiInput(WiiInputType.GuitarWhammy, Model, _microcontroller, Sda, Scl, combined: true),
                        Colors.Transparent,
                        Colors.Transparent, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, StandardAxisType.RightStickX));
                    break;
            }
        }
    }
}