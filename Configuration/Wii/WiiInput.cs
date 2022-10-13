using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Wii;

public class WiiInput : TwiInput
{
    public static readonly string WiiTwiType = "wii";
    public static readonly int WiiTwiFreq = 400000;
    public WiiInputType Input { get; }

    public WiiControllerType WiiControllerType => AxisToType[Input];

    private static readonly Dictionary<WiiInputType, WiiControllerType> AxisToType =
        new()
        {
            {WiiInputType.ClassicLeftStickX, WiiControllerType.ClassicController},
            {WiiInputType.ClassicLeftStickY, WiiControllerType.ClassicController},
            {WiiInputType.ClassicRightStickX, WiiControllerType.ClassicController},
            {WiiInputType.ClassicRightStickY, WiiControllerType.ClassicController},
            {WiiInputType.ClassicLeftTrigger, WiiControllerType.ClassicController},
            {WiiInputType.ClassicRightTrigger, WiiControllerType.ClassicController},
            {WiiInputType.DjCrossfadeSlider, WiiControllerType.Dj},
            {WiiInputType.DjEffectDial, WiiControllerType.Dj},
            {WiiInputType.DjStickX, WiiControllerType.Dj},
            {WiiInputType.DjStickY, WiiControllerType.Dj},
            {WiiInputType.DjTurntableLeft, WiiControllerType.Dj},
            {WiiInputType.DjTurntableRight, WiiControllerType.Dj},
            {WiiInputType.DrawsomePenPressure, WiiControllerType.Drawsome},
            {WiiInputType.DrawsomePenX, WiiControllerType.Drawsome},
            {WiiInputType.DrawsomePenY, WiiControllerType.Drawsome},
            {WiiInputType.UDrawPenPressure, WiiControllerType.UDraw},
            {WiiInputType.UDrawPenX, WiiControllerType.UDraw},
            {WiiInputType.UDrawPenY, WiiControllerType.UDraw},
            {WiiInputType.DrumGreenPressure, WiiControllerType.Drum},
            {WiiInputType.DrumRedPressure, WiiControllerType.Drum},
            {WiiInputType.DrumYellowPressure, WiiControllerType.Drum},
            {WiiInputType.DrumBluePressure, WiiControllerType.Drum},
            {WiiInputType.DrumOrangePressure, WiiControllerType.Drum},
            {WiiInputType.DrumKickPedalPressure, WiiControllerType.Drum},
            {WiiInputType.GuitarJoystickX, WiiControllerType.Guitar},
            {WiiInputType.GuitarJoystickY, WiiControllerType.Guitar},
            {WiiInputType.GuitarTapBar, WiiControllerType.Guitar},
            {WiiInputType.GuitarWhammy, WiiControllerType.Guitar},
            {WiiInputType.NunchukAccelerationX, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukAccelerationY, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukAccelerationZ, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukRotationPitch, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukRotationRoll, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukStickX, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukStickY, WiiControllerType.Nunchuk},
            {WiiInputType.ClassicRt, WiiControllerType.ClassicController},
            {WiiInputType.ClassicPlus, WiiControllerType.ClassicController},
            {WiiInputType.ClassicHome, WiiControllerType.ClassicController},
            {WiiInputType.ClassicMinus, WiiControllerType.ClassicController},
            {WiiInputType.ClassicLt, WiiControllerType.ClassicController},
            {WiiInputType.ClassicDPadDown, WiiControllerType.ClassicController},
            {WiiInputType.ClassicDPadRight, WiiControllerType.ClassicController},
            {WiiInputType.ClassicDPadUp, WiiControllerType.ClassicController},
            {WiiInputType.ClassicDPadLeft, WiiControllerType.ClassicController},
            {WiiInputType.ClassicZr, WiiControllerType.ClassicController},
            {WiiInputType.ClassicX, WiiControllerType.ClassicController},
            {WiiInputType.ClassicA, WiiControllerType.ClassicController},
            {WiiInputType.ClassicY, WiiControllerType.ClassicController},
            {WiiInputType.ClassicB, WiiControllerType.ClassicController},
            {WiiInputType.ClassicZl, WiiControllerType.ClassicController},
            {WiiInputType.DjHeroRightRed, WiiControllerType.Dj},
            {WiiInputType.DjHeroPlus, WiiControllerType.Dj},
            {WiiInputType.DjHeroMinus, WiiControllerType.Dj},
            {WiiInputType.DjHeroLeftRed, WiiControllerType.Dj},
            {WiiInputType.DjHeroRightBlue, WiiControllerType.Dj},
            {WiiInputType.DjHeroLeftGreen, WiiControllerType.Dj},
            {WiiInputType.DjHeroLeftAny, WiiControllerType.Dj},
            {WiiInputType.DjHeroRightAny, WiiControllerType.Dj},
            {WiiInputType.DjHeroEuphoria, WiiControllerType.Dj},
            {WiiInputType.DjHeroRightGreen, WiiControllerType.Dj},
            {WiiInputType.DjHeroLeftBlue, WiiControllerType.Dj},
            {WiiInputType.DrumPlus, WiiControllerType.Drum},
            {WiiInputType.DrumMinus, WiiControllerType.Drum},
            {WiiInputType.DrumKickPedal, WiiControllerType.Drum},
            {WiiInputType.DrumBlue, WiiControllerType.Drum},
            {WiiInputType.DrumGreen, WiiControllerType.Drum},
            {WiiInputType.DrumYellow, WiiControllerType.Drum},
            {WiiInputType.DrumRed, WiiControllerType.Drum},
            {WiiInputType.DrumOrange, WiiControllerType.Drum},
            {WiiInputType.GuitarPlus, WiiControllerType.Guitar},
            {WiiInputType.GuitarMinus, WiiControllerType.Guitar},
            {WiiInputType.GuitarStrumDown, WiiControllerType.Guitar},
            {WiiInputType.GuitarStrumUp, WiiControllerType.Guitar},
            {WiiInputType.GuitarYellow, WiiControllerType.Guitar},
            {WiiInputType.GuitarGreen, WiiControllerType.Guitar},
            {WiiInputType.GuitarBlue, WiiControllerType.Guitar},
            {WiiInputType.GuitarRed, WiiControllerType.Guitar},
            {WiiInputType.GuitarOrange, WiiControllerType.Guitar},
            {WiiInputType.NunchukC, WiiControllerType.Nunchuk},
            {WiiInputType.NunchukZ, WiiControllerType.Nunchuk},
            {WiiInputType.TaTaConRightDrumRim, WiiControllerType.Taiko},
            {WiiInputType.TaTaConRightDrumCenter, WiiControllerType.Taiko},
            {WiiInputType.TaTaConLeftDrumRim, WiiControllerType.Taiko},
            {WiiInputType.TaTaConLeftDrumCenter, WiiControllerType.Taiko},
            {WiiInputType.UDrawPenButton1, WiiControllerType.UDraw},
            {WiiInputType.UDrawPenButton2, WiiControllerType.UDraw},
            {WiiInputType.UDrawPenClick, WiiControllerType.UDraw}
        };

    private static readonly Dictionary<WiiInputType, string> Mappings = new()
    {
        {WiiInputType.ClassicLeftStickX, "((wiiData[0] & 0x3f) - 32) << 9"},
        {WiiInputType.ClassicLeftStickY, "((wiiData[1] & 0x3f) - 32) << 9"},
        {
            WiiInputType.ClassicRightStickX,
            "((((wiiData[0] & 0xc0) >> 3) | ((wiiData[1] & 0xc0) >> 5) | (wiiData[2] >> 7)) -16) << 10"
        },
        {WiiInputType.ClassicRightStickY, "((wiiData[2] & 0x1f) - 16) << 10"},
        {WiiInputType.ClassicLeftTrigger, "((wiiData[3] >> 5) | ((wiiData[2] & 0x60) >> 2))"},
        {WiiInputType.ClassicRightTrigger, "(wiiData[3] & 0x1f) << 3"},
        {WiiInputType.DjCrossfadeSlider, "(wiiData[2] & 0x1E) >> 1"},
        {WiiInputType.DjEffectDial, "(wiiData[3] & 0xE0) >> 5 | (wiiData[2] & 0x60) >> 2"},
        {WiiInputType.DjStickX, "((wiiData[0] & 0x3F) - 0x20) << 10"},
        {WiiInputType.DjStickY, "((wiiData[1] & 0x3F) - 0x20) << 10"},
        {WiiInputType.DjTurntableLeft, "(((wiiData[4] & 1) ? 32 : 1) + (0x1F - (wiiData[3] & 0x1F))) << 10"},
        {
            WiiInputType.DjTurntableRight,
            "(((wiiData[2] & 1) ? 32 : 1) + (0x1F - ((wiiData[2] & 0x80) >> 7 | (wiiData[1] & 0xC0) >> 5 | (wiiData[0] & 0xC0) >> 3))) << 10"
        },
        {WiiInputType.DrawsomePenPressure, "(wiiData[4] | (wiiData[5] & 0x0f) << 8)"},
        {WiiInputType.DrawsomePenX, "wiiData[0] | wiiData[1] << 8"},
        {WiiInputType.DrawsomePenY, "wiiData[2] | wiiData[3] << 8"},
        {WiiInputType.UDrawPenPressure, "wiiData[3]"},
        {WiiInputType.UDrawPenX, "((wiiData[2] & 0x0f) << 8) | wiiData[0]"},
        {WiiInputType.UDrawPenY, "((wiiData[2] & 0xf0) << 4) | wiiData[1]"},
        {WiiInputType.DrumGreenPressure, "drumVelocity[DRUM_GREEN]"},
        {WiiInputType.DrumRedPressure, "drumVelocity[DRUM_RED]"},
        {WiiInputType.DrumYellowPressure, "drumVelocity[DRUM_YELLOW]"},
        {WiiInputType.DrumBluePressure, "drumVelocity[DRUM_BLUE]"},
        {WiiInputType.DrumOrangePressure, "drumVelocity[DRUM_ORANGE]"},
        {WiiInputType.DrumKickPedalPressure, "drumVelocity[DRUM_KICK]"},
        {WiiInputType.GuitarJoystickX, "((wiiData[0] & 0x3f) - 32) << 10"},
        {WiiInputType.GuitarJoystickY, "((wiiData[1] & 0x3f) - 32) << 10"},
        {WiiInputType.GuitarTapBar, "(wiiData[2] & 0x1f) << 11"},
        {WiiInputType.GuitarWhammy, "(wiiData[3] & 0x1f) << 11"},
        {WiiInputType.NunchukAccelerationX, "accX"},
        {WiiInputType.NunchukAccelerationY, "accY"},
        {WiiInputType.NunchukAccelerationZ, "accZ"},
        {WiiInputType.NunchukRotationPitch, $"fxpt_atan2(accY,accZ)"},
        {WiiInputType.NunchukRotationRoll, $"fxpt_atan2(accX,accZ)"},
        {WiiInputType.NunchukStickX, "(wiiData[0] - 0x80) << 8"},
        {WiiInputType.NunchukStickY, "(wiiData[1] - 0x80) << 8"},
        {WiiInputType.ClassicRt, "((~wiiData[4]) & (1 << 1))"},
        {WiiInputType.ClassicPlus, "((~wiiData[4]) & (1 << 2))"},
        {WiiInputType.ClassicHome, "((~wiiData[4]) & (1 << 3))"},
        {WiiInputType.ClassicMinus, "((~wiiData[4]) & (1 << 4))"},
        {WiiInputType.ClassicLt, "((~wiiData[4]) & (1 << 5))"},
        {WiiInputType.ClassicDPadDown, "((~wiiData[4]) & (1 << 6))"},
        {WiiInputType.ClassicDPadRight, "((~wiiData[4]) & (1 << 7))"},
        {WiiInputType.ClassicDPadUp, "((~wiiData[5]) & (1 << 0))"},
        {WiiInputType.ClassicDPadLeft, "((~wiiData[5]) & (1 << 1))"},
        {WiiInputType.ClassicZr, "((~wiiData[5]) & (1 << 2))"},
        {WiiInputType.ClassicX, "((~wiiData[5]) & (1 << 3))"},
        {WiiInputType.ClassicA, "((~wiiData[5]) & (1 << 4))"},
        {WiiInputType.ClassicY, "((~wiiData[5]) & (1 << 5))"},
        {WiiInputType.ClassicB, "((~wiiData[5]) & (1 << 6))"},
        {WiiInputType.ClassicZl, "((~wiiData[5]) & (1 << 7))"},
        {WiiInputType.DjHeroPlus, "((~wiiData[4]) & (1 << 2))"},
        {WiiInputType.DjHeroMinus, "((~wiiData[4]) & (1 << 4))"},
        {WiiInputType.DjHeroLeftBlue, "((~wiiData[5]) & (1 << 7))"},
        {WiiInputType.DjHeroLeftRed, "((~wiiData[4]) & (1 << 5))"},
        {WiiInputType.DjHeroLeftGreen, "((~wiiData[5]) & (1 << 3))"},
        {WiiInputType.DjHeroLeftAny, "(((~wiiData[5]) & ((1 << 3)|1 << 7)) | ((~wiiData[4]) & (1 << 5)))"},
        {WiiInputType.DjHeroRightGreen, "((~wiiData[5]) & (1 << 5))"},
        {WiiInputType.DjHeroRightRed, "((~wiiData[4]) & (1 << 1))"},
        {WiiInputType.DjHeroRightBlue, "((~wiiData[5]) & (1 << 2))"},
        {WiiInputType.DjHeroRightAny, "(((~wiiData[5]) & ((1 << 5)|1 << 2)) | ((~wiiData[4]) & (1 << 1)))"},
        {WiiInputType.DjHeroEuphoria, "((~wiiData[5]) & (1 << 4))"},
        {WiiInputType.DrumPlus, "((~wiiData[4]) & (1 << 2))"},
        {WiiInputType.DrumMinus, "((~wiiData[4]) & (1 << 4))"},
        {WiiInputType.DrumKickPedal, "((~wiiData[5]) & (1 << 2))"},
        {WiiInputType.DrumBlue, "((~wiiData[5]) & (1 << 3))"},
        {WiiInputType.DrumGreen, "((~wiiData[5]) & (1 << 4))"},
        {WiiInputType.DrumYellow, "((~wiiData[5]) & (1 << 5))"},
        {WiiInputType.DrumRed, "((~wiiData[5]) & (1 << 6))"},
        {WiiInputType.DrumOrange, "((~wiiData[5]) & (1 << 7))"},
        {WiiInputType.GuitarPlus, "((~wiiData[4]) & (1 << 2))"},
        {WiiInputType.GuitarMinus, "((~wiiData[4]) & (1 << 4))"},
        {WiiInputType.GuitarStrumDown, "((~wiiData[4]) & (1 << 6))"},
        {WiiInputType.GuitarStrumUp, "((~wiiData[5]) & (1 << 0))"},
        {WiiInputType.GuitarYellow, "((~wiiData[5]) & (1 << 3))"},
        {WiiInputType.GuitarGreen, "((~wiiData[5]) & (1 << 4))"},
        {WiiInputType.GuitarBlue, "((~wiiData[5]) & (1 << 5))"},
        {WiiInputType.GuitarRed, "((~wiiData[5]) & (1 << 6))"},
        {WiiInputType.GuitarOrange, "((~wiiData[5]) & (1 << 7))"},
        {WiiInputType.NunchukC, "((~wiiData[5]) & (1 << 1))"},
        {WiiInputType.NunchukZ, "((~wiiData[5]) & (1 << 0))"},
        {WiiInputType.TaTaConRightDrumRim, "((~wiiData[0]) & (1 << 3))"},
        {WiiInputType.TaTaConRightDrumCenter, "((~wiiData[0]) & (1 << 4))"},
        {WiiInputType.TaTaConLeftDrumRim, "((~wiiData[0]) & (1 << 5))"},
        {WiiInputType.TaTaConLeftDrumCenter, "((~wiiData[0]) & (1 << 6))"},
        {WiiInputType.UDrawPenButton1, "((~wiiData[5]) & (1 << 0))"},
        {WiiInputType.UDrawPenButton2, "((~wiiData[5]) & (1 << 1))"},
        {WiiInputType.UDrawPenClick, "(( wiiData[5]) & (1 << 2))"}, 
        {WiiInputType.GuitarTapGreen, GetMappingForTapBar(0x04, 0x07)},
        {WiiInputType.GuitarTapRed, GetMappingForTapBar(0x07, 0x0A, 0x0c, 0x0d)},
        {WiiInputType.GuitarTapYellow, GetMappingForTapBar(0x0c, 0x0d, 0x12, 0x13, 0x14, 0x15)},
        {WiiInputType.GuitarTapBlue, GetMappingForTapBar(0x14, 0x15, 0x17, 0x18, 0x1A)},
        {WiiInputType.GuitarTapOrange, GetMappingForTapBar(0x1A, 0x1F)},
    };

    private static readonly List<string> HiResMapOrder = new()
    {
        "wiiData[4]",
        "wiiData[5]",
        Mappings[WiiInputType.ClassicLeftStickX],
        Mappings[WiiInputType.ClassicLeftStickY],
        Mappings[WiiInputType.ClassicRightStickX],
        Mappings[WiiInputType.ClassicRightStickY],
        Mappings[WiiInputType.ClassicLeftTrigger],
        Mappings[WiiInputType.ClassicRightTrigger],
    };

    private static readonly Dictionary<string, string> HiResMap = new()
    {
        {"wiiData[4]", "wiiData[6]"},
        {"wiiData[5]", "wiiData[7]"},
        {Mappings[WiiInputType.ClassicLeftStickX], "(wiiData[0] - 0x80) << 8"},
        {Mappings[WiiInputType.ClassicLeftStickY], "(wiiData[2] - 0x80) << 8"},
        {Mappings[WiiInputType.ClassicRightStickX], "(wiiData[1] - 0x80) << 8"},
        {Mappings[WiiInputType.ClassicRightStickY], "(wiiData[3] - 0x80) << 8"},
        {Mappings[WiiInputType.ClassicLeftTrigger], "wiiData[4]"},
        {Mappings[WiiInputType.ClassicRightTrigger], "wiiData[5]"},
    };

    private static readonly Dictionary<WiiControllerType, string> CType = new()
    {
        {WiiControllerType.ClassicController, "WII_CLASSIC_CONTROLLER"},
        {WiiControllerType.Nunchuk, "WII_NUNCHUK"},
        {WiiControllerType.UDraw, "WII_THQ_UDRAW_TABLET"},
        {WiiControllerType.Drawsome, "WII_UBISOFT_DRAWSOME_TABLET"},
        {WiiControllerType.Guitar, "WII_GUITAR_HERO_GUITAR_CONTROLLER"},
        {WiiControllerType.Drum, "WII_GUITAR_HERO_DRUM_CONTROLLER"},
        {WiiControllerType.Dj, "WII_DJ_HERO_TURNTABLE"},
        {WiiControllerType.Taiko, "WII_TAIKO_NO_TATSUJIN_CONTROLLER"},
        {WiiControllerType.MotionPlus, "WII_MOTION_PLUS"}
    };

    public WiiInput(WiiInputType input, Microcontroller microcontroller, int? sda = null, int? scl = null) : base(
        microcontroller,
        WiiTwiType, WiiTwiFreq, sda, scl)
    {
        Input = input;
    }

    public override string Generate()
    {
        return Mappings[Input];
    }

    public override SerializedInput GetJson()
    {
        return new SerializedWiiInput(Sda, Scl, Input);
    }

    public override bool IsAnalog => Input <= WiiInputType.DrawsomePenPressure;
    public override bool IsUint => !Input.ToString().ToLower().Contains("stick");

    public override List<DevicePin> Pins => new();

    private static string GetMappingForTapBar(params int[] mappings)
    {
        return string.Join(" || ", mappings.Select(s2 => $"(lastTapWii == {s2})"));
    }

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller)
    {
        Dictionary<WiiControllerType, List<string>> mappedBindings = new();
        bool hasTapBar = false;
        foreach (var binding in bindings)
        {
            if (binding.Item1 is WiiInput input)
            {
                if (!mappedBindings.ContainsKey(input.WiiControllerType))
                {
                    mappedBindings.Add(input.WiiControllerType, new List<string>());
                }

                if (input.Input.ToString().StartsWith("GuitarTap"))
                {
                    hasTapBar = true;
                }

                mappedBindings[input.WiiControllerType].Add(binding.Item2);
            }
        }

        var ret = "if (wiiValid) {";
        if (hasTapBar)
        {
            ret += "uint8_t lastTapWii = (wiiData[2] & 0x1f) << 11;";
        }

        ret += " switch(wiiControllerType) {";
        if (mappedBindings.ContainsKey(WiiControllerType.ClassicController))
        {
            var mappings = mappedBindings[WiiControllerType.ClassicController];
            mappedBindings.Remove(WiiControllerType.ClassicController);
            var mappings2 = new List<string>();
            foreach (var mapping in mappings)
            {
                var val = mapping;
                foreach (var key in HiResMapOrder)
                {
                    val = val.Replace(key, HiResMap[key]);
                }

                mappings2.Add(val);
            }

            ret += @$"
case WII_CLASSIC_CONTROLLER:
case WII_CLASSIC_CONTROLLER_PRO:
if (hiRes) {{
    {String.Join(";\n", mappings2)};
}} else {{
    {String.Join(";\n", mappings)};
}}
break;
";
        }

        foreach (var binding in mappedBindings)
        {
            var input = binding.Key;
            var mappings = binding.Value;
            ret += @$"case {CType[input]}:
    {String.Join(";\n", mappings)};
    break;";
        }

        return ret + "}}";
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        if (WiiControllerType == WiiControllerType.Drum)
        {
            return base.RequiredDefines().Concat(new[] {"INPUT_WII", "INPUT_WII_DRUM"}).ToList();
        }

        if (WiiControllerType == WiiControllerType.Nunchuk)
        {
            return base.RequiredDefines().Concat(new[] {"INPUT_WII", "INPUT_WII_NUNCHUK"}).ToList();
        }

        return base.RequiredDefines().Concat(new[] {"INPUT_WII"}).ToList();
    }
}