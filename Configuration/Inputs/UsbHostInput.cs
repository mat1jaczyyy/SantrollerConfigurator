using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class UsbHostInput : Input
{
    public UsbHostInput(ConfigViewModel model, UsbHostInputType input, bool combined = false) : base(model)
    {
        Combined = combined;
        Input = input;
        _usbHostDm = model.WhenAnyValue(x => x.UsbHostDm).ToProperty(this, x => x.UsbHostDm);
        _usbHostDp = model.WhenAnyValue(x => x.UsbHostDp).ToProperty(this, x => x.UsbHostDp);
    }

    public bool Combined { get; }

    public UsbHostInputType Input { get; }

    public override bool IsUint => Input is not (UsbHostInputType.LeftStickX or UsbHostInputType.LeftStickY
        or UsbHostInputType.RightStickX or UsbHostInputType.RightStickY or UsbHostInputType.Crossfader
        or UsbHostInputType.LeftTableVelocity or UsbHostInputType.RightTableVelocity
        or UsbHostInputType.EffectsKnob);

    public override IList<DevicePin> Pins => Array.Empty<DevicePin>();
    public override IList<PinConfig> PinConfigs => Model.UsbHostPinConfigs();
    public override InputType? InputType => Types.InputType.UsbHostInput;
    public override string Title => EnumToStringConverter.Convert(Input);

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => Model.AvailablePins.Skip(1).ToList();
    public List<int> AvailablePinsDp => Model.AvailablePins.SkipLast(1).ToList();
    private readonly ObservableAsPropertyHelper<int> _usbHostDm;
    private readonly ObservableAsPropertyHelper<int> _usbHostDp;

    [Reactive] public string UsbHostInfo { get; set; } = "";
    [Reactive] public int ConnectedDevices { get; set; }
    public int UsbHostDm
    {
        get => _usbHostDm.Value;
        set => Model.UsbHostDm = value;
    }
    
    public int UsbHostDp
    {
        get => _usbHostDp.Value;
        set => Model.UsbHostDp = value;
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_USB_HOST"};
    }

    public override string Generate()
    {
        return Output.GetReportField(Input, "usb_host_data");
    }

    public override SerializedInput Serialise()
    {
        return new SerializedUsbHostInput(Input, Combined);
    }

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] usbHostInputsRaw, byte[] usbHostRaw)
    {
        var buffer = "";
        // When combined, the combined output renders this, so we don't need to calculate it
        if (!Combined && usbHostRaw.Any())
        {
            for (var i = 0; i < usbHostRaw.Length; i += 3)
            {
                var consoleType = (ConsoleType) usbHostRaw[i];
                string subType;
                var rhythmType = "";
                if (consoleType == ConsoleType.Xbox360)
                {
                    var xInputSubType = (XInputSubType) usbHostRaw[i + 1];
                    subType = EnumToStringConverter.Convert(xInputSubType);
                    if (xInputSubType is XInputSubType.Drums or XInputSubType.Guitar or XInputSubType.GuitarAlternate)
                        rhythmType = " " + EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
                }
                else
                {
                    var deviceType = (DeviceControllerType) usbHostRaw[i + 1];
                    subType = EnumToStringConverter.Convert(deviceType);
                    if (deviceType is DeviceControllerType.Drum or DeviceControllerType.Guitar)
                        rhythmType = " " + EnumToStringConverter.Convert((RhythmType) usbHostRaw[i + 2]);
                }

                buffer += $"{consoleType} {rhythmType} {subType}\n";
            }

            ConnectedDevices = usbHostRaw.Length / 3;

            UsbHostInfo = buffer.Trim();
        }

        if (usbHostInputsRaw.Length < Marshal.SizeOf<UsbHostInputs>()) return;
        var inputs = StructTools.RawDeserialize<UsbHostInputs>(usbHostInputsRaw, 0);
        RawValue = inputs.RawValue(Input);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings, ConfigField mode)
    {
        return string.Join("\n", bindings.Select(binding => binding.Item2));
    }

    

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UsbHostInputs
    {
        private readonly uint buttons;
        private readonly byte buttons2;

        private bool ButtonPressed(UsbHostInputType inputType)
        {
            if (inputType >= UsbHostInputType.LeftTrigger) return false;
            var val = (uint) inputType + 1;
            if (val >= 32)
            {
                val -= 32;
                return (buttons2 & (1 << (int) val)) != 0;
            }

            return (buttons & (1 << (int) val)) != 0;
        }

        private readonly ushort leftTrigger;
        private readonly ushort rightTrigger;
        private readonly short leftStickX;
        private readonly short leftStickY;
        private readonly short rightStickX;
        private readonly short rightStickY;
        private readonly byte pressureDpadUp;
        private readonly byte pressureDpadRight;
        private readonly byte pressureDpadLeft;
        private readonly byte pressureDpadDown;
        private readonly byte pressureL1;
        private readonly byte pressureR1;
        private readonly byte pressureTriangle;
        private readonly byte pressureCircle;
        private readonly byte pressureCross;
        private readonly byte pressureSquare;
        private readonly byte redVelocity;
        private readonly byte yellowVelocity;
        private readonly byte blueVelocity;
        private readonly byte greenVelocity;
        private readonly byte orangeVelocity;
        private readonly byte blueCymbalVelocity;
        private readonly byte yellowCymbalVelocity;
        private readonly byte greenCymbalVelocity;
        private readonly byte kickVelocity;
        private readonly byte whammy;
        private readonly byte tilt;
        private readonly byte pickup;
        private readonly byte slider;
        private readonly short leftTableVelocity;
        private readonly short rightTableVelocity;
        private readonly short effectsKnob;
        private readonly short crossfader;
        private readonly ushort accelX;
        private readonly ushort accelZ;
        private readonly ushort accelY;
        private readonly ushort gyro;

        public int RawValue(UsbHostInputType inputType)
        {
            return inputType switch
            {
                UsbHostInputType.LeftTrigger => leftTrigger,
                UsbHostInputType.RightTrigger => rightTrigger,
                UsbHostInputType.LeftStickX => leftStickX,
                UsbHostInputType.LeftStickY => leftStickY,
                UsbHostInputType.RightStickX => rightStickX,
                UsbHostInputType.RightStickY => rightStickY,
                UsbHostInputType.PressureDpadUp => pressureDpadUp,
                UsbHostInputType.PressureDpadRight => pressureDpadRight,
                UsbHostInputType.PressureDpadLeft => pressureDpadLeft,
                UsbHostInputType.PressureDpadDown => pressureDpadDown,
                UsbHostInputType.PressureL1 => pressureL1,
                UsbHostInputType.PressureR1 => pressureR1,
                UsbHostInputType.PressureTriangle => pressureTriangle,
                UsbHostInputType.PressureCircle => pressureCircle,
                UsbHostInputType.PressureCross => pressureCross,
                UsbHostInputType.PressureSquare => pressureSquare,
                UsbHostInputType.RedVelocity => redVelocity,
                UsbHostInputType.YellowVelocity => yellowVelocity,
                UsbHostInputType.BlueVelocity => blueVelocity,
                UsbHostInputType.GreenVelocity => greenVelocity,
                UsbHostInputType.OrangeVelocity => orangeVelocity,
                UsbHostInputType.BlueCymbalVelocity => blueCymbalVelocity,
                UsbHostInputType.YellowCymbalVelocity => yellowCymbalVelocity,
                UsbHostInputType.GreenCymbalVelocity => greenCymbalVelocity,
                UsbHostInputType.KickVelocity => kickVelocity,
                UsbHostInputType.Whammy => whammy,
                UsbHostInputType.Tilt => tilt,
                UsbHostInputType.Pickup => pickup,
                UsbHostInputType.Slider => slider,
                UsbHostInputType.LeftTableVelocity => leftTableVelocity,
                UsbHostInputType.RightTableVelocity => rightTableVelocity,
                UsbHostInputType.EffectsKnob => effectsKnob,
                UsbHostInputType.Crossfader => crossfader,
                UsbHostInputType.AccelX => accelX,
                UsbHostInputType.AccelZ => accelZ,
                UsbHostInputType.AccelY => accelY,
                UsbHostInputType.Gyro => gyro,
                _ => ButtonPressed(inputType) ? 1 : 0
            };
        }
    }
}