using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public class Ps2Input : SpiInput
{
    public static readonly string Ps2SpiType = "ps2";
    public static readonly uint Ps2SpiFreq = 500000;
    public static readonly bool Ps2SpiCpol = true;
    public static readonly bool Ps2SpiCpha = true;
    public static readonly bool Ps2SpiMsbFirst = false;
    public static readonly string Ps2AckType = "ps2_ack";
    public static readonly string Ps2AttType = "ps2_att";

    public static readonly List<Ps2InputType> Dualshock2Order = new()
    {
        Ps2InputType.RightX,
        Ps2InputType.RightY,
        Ps2InputType.LeftX,
        Ps2InputType.LeftY,
        Ps2InputType.Dualshock2RightButton,
        Ps2InputType.Dualshock2LeftButton,
        Ps2InputType.Dualshock2UpButton,
        Ps2InputType.Dualshock2DownButton,
        Ps2InputType.Dualshock2Triangle,
        Ps2InputType.Dualshock2Circle,
        Ps2InputType.Dualshock2Cross,
        Ps2InputType.Dualshock2Square,
        Ps2InputType.Dualshock2L1,
        Ps2InputType.Dualshock2R1,
        Ps2InputType.Dualshock2L2,
        Ps2InputType.Dualshock2R2
    };

    public static readonly List<Ps2InputType> Dualshock2AnalogButtons = new()
    {
        Ps2InputType.Dualshock2RightButton,
        Ps2InputType.Dualshock2LeftButton,
        Ps2InputType.Dualshock2UpButton,
        Ps2InputType.Dualshock2DownButton,
        Ps2InputType.Dualshock2Triangle,
        Ps2InputType.Dualshock2Circle,
        Ps2InputType.Dualshock2Cross,
        Ps2InputType.Dualshock2Square,
        Ps2InputType.Dualshock2L1,
        Ps2InputType.Dualshock2R1,
        Ps2InputType.Dualshock2L2,
        Ps2InputType.Dualshock2R2
    };

    public static readonly List<Ps2InputType> GuitarButtons = new()
    {
        Ps2InputType.GuitarGreen,
        Ps2InputType.GuitarRed,
        Ps2InputType.GuitarYellow,
        Ps2InputType.GuitarBlue,
        Ps2InputType.GuitarOrange,
        Ps2InputType.GuitarSelect,
        Ps2InputType.GuitarStart,
        Ps2InputType.GuitarTilt,
        Ps2InputType.GuitarStrumUp,
        Ps2InputType.GuitarStrumDown
    };

    public static readonly List<Ps2InputType> DigitalButtons = new()
    {
        Ps2InputType.L3,
        Ps2InputType.R3,
        Ps2InputType.Start,
        Ps2InputType.Select,
        Ps2InputType.DPadUp,
        Ps2InputType.DPadRight,
        Ps2InputType.DPadDown,
        Ps2InputType.DPadLeft,
        Ps2InputType.L2,
        Ps2InputType.R2,
        Ps2InputType.L1,
        Ps2InputType.R1,
        Ps2InputType.Triangle,
        Ps2InputType.Circle,
        Ps2InputType.Cross,
        Ps2InputType.Square
    };

    private static readonly List<Ps2InputType> IntInputs = new()
    {
        Ps2InputType.LeftX,
        Ps2InputType.LeftY,
        Ps2InputType.MouseX,
        Ps2InputType.MouseY,
        Ps2InputType.RightX,
        Ps2InputType.RightY,
        Ps2InputType.NegConTwist
    };

    private static readonly Dictionary<Ps2InputType, string> Mappings = new()
    {
        {Ps2InputType.LeftX, "(ps2Data[7] - 128) << 8"},
        {Ps2InputType.LeftY, "-(ps2Data[8] - 127) << 8"},
        {Ps2InputType.MouseX, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.MouseY, "-(ps2Data[6] - 127) << 8"},
        {Ps2InputType.RightX, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.RightY, "-(ps2Data[6] - 127) << 8"},
        {Ps2InputType.NegConTwist, "(ps2Data[5] - 128) << 8"},
        {Ps2InputType.NegConI, "ps2Data[6]"},
        {Ps2InputType.NegConIi, "ps2Data[7]"},
        {Ps2InputType.NegConL, "ps2Data[8]"},
        {Ps2InputType.NegConR, "(~ps2Data[4]) & (1 << 3)"},
        {Ps2InputType.NegConA, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.NegConB, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.GunconHSync, "(ps2Data[6] << 8) | ps2Data[5]"},
        {Ps2InputType.GunconVSync, "(ps2Data[8] << 8) | ps2Data[7]"},
        {Ps2InputType.JogConWheel, "(ps2Data[6] << 8) | ps2Data[5]"},
        {Ps2InputType.GuitarWhammy, "-(ps2Data[8] - 127) << 9"},
        {Ps2InputType.Dualshock2RightButton, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2LeftButton, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2UpButton, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2DownButton, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2Triangle, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2Circle, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2Cross, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2Square, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2L1, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2R1, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2L2, "ps2Data[generated]"},
        {Ps2InputType.Dualshock2R2, "ps2Data[generated]"},
        {Ps2InputType.GuitarGreen, "(~ps2Data[4]) & (1 << 1)"},
        {Ps2InputType.GuitarRed, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.GuitarYellow, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.GuitarBlue, "(~ps2Data[4]) & (1 << 6)"},
        {Ps2InputType.GuitarOrange, "(~ps2Data[4]) & (1 << 7)"},
        {Ps2InputType.GuitarTilt, "(~ps2Data[4]) & (1 << 0)"},
        {Ps2InputType.GuitarSelect, "(~ps2Data[3]) & (1 << 0)"},
        {Ps2InputType.GuitarStart, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.NegConStart, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.L3, "(~ps2Data[3]) & (1 << 1)"},
        {Ps2InputType.R3, "(~ps2Data[3]) & (1 << 2)"},
        {Ps2InputType.Start, "(~ps2Data[3]) & (1 << 3)"},
        {Ps2InputType.GuitarStrumUp, "(~ps2Data[3]) & (1 << 4)"},
        {Ps2InputType.GuitarStrumDown, "(~ps2Data[3]) & (1 << 6)"},
        {Ps2InputType.Select, "(~ps2Data[3]) & (1 << 0)"},
        {Ps2InputType.DPadUp, "(~ps2Data[3]) & (1 << 4)"},
        {Ps2InputType.DPadRight, "(~ps2Data[3]) & (1 << 5)"},
        {Ps2InputType.DPadDown, "(~ps2Data[3]) & (1 << 6)"},
        {Ps2InputType.DPadLeft, "(~ps2Data[3]) & (1 << 7)"},
        {Ps2InputType.L2, "(~ps2Data[4]) & (1 << 0)"},
        {Ps2InputType.R2, "(~ps2Data[4]) & (1 << 1)"},
        {Ps2InputType.L1, "(~ps2Data[4]) & (1 << 2)"},
        {Ps2InputType.R1, "(~ps2Data[4]) & (1 << 3)"},
        {Ps2InputType.Triangle, "(~ps2Data[4]) & (1 << 4)"},
        {Ps2InputType.Circle, "(~ps2Data[4]) & (1 << 5)"},
        {Ps2InputType.Cross, "(~ps2Data[4]) & (1 << 6)"},
        {Ps2InputType.Square, "(~ps2Data[4]) & (1 << 7)"}
    };

    private static readonly Dictionary<Ps2InputType, Ps2ControllerType> AxisToType =
        new()
        {
            {Ps2InputType.GunconHSync, Ps2ControllerType.JogCon},
            {Ps2InputType.GunconVSync, Ps2ControllerType.JogCon},
            {Ps2InputType.MouseX, Ps2ControllerType.Mouse},
            {Ps2InputType.MouseY, Ps2ControllerType.Mouse},
            {Ps2InputType.NegConTwist, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConI, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConIi, Ps2ControllerType.NegCon},
            {Ps2InputType.NegConL, Ps2ControllerType.NegCon},
            {Ps2InputType.JogConWheel, Ps2ControllerType.JogCon},
            {Ps2InputType.GuitarWhammy, Ps2ControllerType.Guitar}
        };

    private static readonly IReadOnlyList<Ps2InputType> Dualshock = new[]
    {
        Ps2InputType.LeftX,
        Ps2InputType.LeftY,
        Ps2InputType.RightX,
        Ps2InputType.RightY
    };

    private static readonly Dictionary<Ps2ControllerType, string> CType = new()
    {
        {Ps2ControllerType.Digital, "PSX_DIGITAL"},
        {Ps2ControllerType.Dualshock, "PSX_DUALSHOCK_1_CONTROLLER"},
        {Ps2ControllerType.Dualshock2, "PSX_DUALSHOCK_2_CONTROLLER"},
        {Ps2ControllerType.FlightStick, "PSX_FLIGHTSTICK"},
        {Ps2ControllerType.NegCon, "PSX_NEGCON"},
        {Ps2ControllerType.JogCon, "PSX_JOGCON"},
        {Ps2ControllerType.GunCon, "PSX_GUNCON"},
        {Ps2ControllerType.Guitar, "PSX_GUITAR_HERO_CONTROLLER"},
        {Ps2ControllerType.Mouse, "PSX_MOUSE"}
    };

    private readonly DirectPinConfig _ackConfig;
    private readonly DirectPinConfig _attConfig;

    public Ps2Input(Ps2InputType input, ConfigViewModel model, int miso = -1,
        int mosi = -1,
        int sck = -1, int att = -1, int ack = -1, bool combined = false) : base(
        Ps2SpiType,
        Ps2SpiFreq, Ps2SpiCpol,
        Ps2SpiCpha, Ps2SpiMsbFirst, miso: miso, mosi: mosi, sck: sck, model: model)
    {
        Combined = combined;
        BindableSpi = !Combined && Model.Microcontroller.SpiAssignable;
        Input = input;
        _ackConfig = Model.GetPinForType(Ps2AckType, ack, DevicePinMode.Floating);
        _attConfig = Model.GetPinForType(Ps2AttType, ack, DevicePinMode.Floating);
        this.WhenAnyValue(x => x._attConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Att)));
        this.WhenAnyValue(x => x._ackConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Ack)));
        IsAnalog = Input <= Ps2InputType.Dualshock2R2;
    }

    public int Ack
    {
        get => _ackConfig.Pin;
        set => _ackConfig.Pin = value;
    }

    public int Att
    {
        get => _attConfig.Pin;
        set => _attConfig.Pin = value;
    }

    public Ps2InputType Input { get; }

    public bool Combined { get; }

    public bool BindableSpi { get; }

    public override bool IsUint => !IntInputs.Contains(Input);

    public override InputType? InputType => Types.InputType.Ps2Input;
    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(false);

    public override IList<DevicePin> Pins => new List<DevicePin>
    {
        new(Att, DevicePinMode.Output),
        new(Ack, DevicePinMode.Floating)
    };

    public override IList<PinConfig> PinConfigs =>
        base.PinConfigs.Concat(new List<PinConfig> {_ackConfig, _attConfig}).ToList();

    public override string Title => EnumToStringConverter.Convert(Input);

    public override string Generate()
    {
        return Mappings[Input];
    }

    public override SerializedInput Serialise()
    {
        if (Combined)
            return new SerializedPs2InputCombined(Input);
        return new SerializedPs2Input(Miso, Mosi, Sck, Att, Ack, Input);
    }

    public static string GeneratePs2Pressures(List<Input> bindings)
    {
        var retDs2 = "#define PRESSURES_DS2 0b11";
        var ds2Axis = bindings.Where(s => s is Ps2Input).Cast<Ps2Input>().Select(s => s.Input).ToHashSet();
        var found = false;
        for (var i = 0; i < Dualshock2Order.Count; i++)
        {
            found = true;
            var binding = Dualshock2Order[i];
            if (ds2Axis.Contains(binding))
                retDs2 += "1";
            else
                retDs2 += "0";

            if ((i + 3) % 8 == 0 && i != Dualshock2Order.Count) retDs2 += ", 0b";
        }

        if (!found) return "";

        return retDs2;
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Data,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] usbHostInputsRaw, byte[] usbHostRaw)
    {
        if (!ps2ControllerType.Any() || !ps2Data.Any()) return;
        var type = ps2ControllerType[0];
        if (!Enum.IsDefined(typeof(Ps2ControllerType), type)) return;
        var realType = (Ps2ControllerType) type;
        var mouse = realType == Ps2ControllerType.Mouse;
        var guitar = realType == Ps2ControllerType.Guitar;
        var basicAxis = realType is Ps2ControllerType.Dualshock or Ps2ControllerType.Dualshock2
            or Ps2ControllerType.FlightStick;
        var digital = realType is Ps2ControllerType.Dualshock or Ps2ControllerType.Dualshock2
            or Ps2ControllerType.FlightStick or Ps2ControllerType.Digital;
        var negcon = realType is Ps2ControllerType.NegCon;
        var jogcon = realType is Ps2ControllerType.JogCon;
        var guncon = realType is Ps2ControllerType.GunCon;
        // TODO: perhaps for this, we just swap to a version that polls all PS2 inputs when updating the gui, instead of this.
        // TODO: otherwise, this is actually useless, what we need is the last written config not the current one
        // var ds2 = realType is Ps2ControllerType.Dualshock2;
        // if (ds2 && Dualshock2Order.Contains(Input))
        // {
        //     var inputs = modelBindings.SelectMany(s => s.Outputs.Items).Select(s => s.Input).OfType<Ps2Input>()
        //         .Select(s => s.Input).ToHashSet();
        //     var i = Dualshock2Order.Intersect(inputs).Select((s, idx) => (s, idx))
        //         .FirstOrOptional(s => s.s == Input);
        //     if (i.HasValue)
        //     {
        //         RawValue = ps2Data[i.Value.idx] << 8;
        //         return;
        //     }
        // }

        RawValue = Input switch
        {
            Ps2InputType.MouseX when mouse => (ps2Data[5] - 128) << 8,
            Ps2InputType.MouseY when mouse => -(ps2Data[6] - 127) << 8,
            Ps2InputType.LeftX when basicAxis => (ps2Data[7] - 128) << 8,
            Ps2InputType.LeftY when basicAxis => -(ps2Data[8] - 127) << 8,
            Ps2InputType.RightX when basicAxis => (ps2Data[5] - 128) << 8,
            Ps2InputType.RightY when basicAxis => -(ps2Data[6] - 127) << 8,
            Ps2InputType.NegConTwist when negcon => (ps2Data[5] - 128) << 8,
            Ps2InputType.NegConI when negcon => ps2Data[6],
            Ps2InputType.NegConIi when negcon => ps2Data[7],
            Ps2InputType.NegConL when negcon => ps2Data[8],
            Ps2InputType.NegConR when negcon => ~ps2Data[4] & (1 << 3),
            Ps2InputType.NegConA when negcon => ~ps2Data[4] & (1 << 5),
            Ps2InputType.NegConB when negcon => ~ps2Data[4] & (1 << 4),
            Ps2InputType.GunconHSync when guncon => (ps2Data[6] << 8) | ps2Data[5],
            Ps2InputType.GunconVSync when guncon => (ps2Data[8] << 8) | ps2Data[7],
            Ps2InputType.JogConWheel when jogcon => (ps2Data[6] << 8) | ps2Data[5],
            Ps2InputType.GuitarWhammy when guitar => -(ps2Data[8] - 127) << 9,
            Ps2InputType.GuitarGreen when guitar => ~ps2Data[4] & (1 << 1),
            Ps2InputType.GuitarRed when guitar => ~ps2Data[4] & (1 << 5),
            Ps2InputType.GuitarYellow when guitar => ~ps2Data[4] & (1 << 4),
            Ps2InputType.GuitarBlue when guitar => ~ps2Data[4] & (1 << 6),
            Ps2InputType.GuitarOrange when guitar => ~ps2Data[4] & (1 << 7),
            Ps2InputType.GuitarSelect when guitar => ~ps2Data[3] & (1 << 0),
            Ps2InputType.GuitarTilt when guitar => ~ps2Data[4] & (1 << 0),
            Ps2InputType.GuitarStart when guitar => ~ps2Data[3] & (1 << 3),
            Ps2InputType.GuitarStrumUp when guitar => ~ps2Data[3] & (1 << 4),
            Ps2InputType.GuitarStrumDown when guitar => ~ps2Data[3] & (1 << 6),
            Ps2InputType.NegConStart => ~ps2Data[3] & (1 << 3),
            Ps2InputType.L3 when digital => ~ps2Data[3] & (1 << 1),
            Ps2InputType.R3 when digital => ~ps2Data[3] & (1 << 2),
            Ps2InputType.Start when digital => ~ps2Data[3] & (1 << 3),
            Ps2InputType.Select when digital => ~ps2Data[3] & (1 << 0),
            Ps2InputType.DPadUp when digital => ~ps2Data[3] & (1 << 4),
            Ps2InputType.DPadRight when digital => ~ps2Data[3] & (1 << 5),
            Ps2InputType.DPadDown when digital => ~ps2Data[3] & (1 << 6),
            Ps2InputType.DPadLeft when digital => ~ps2Data[3] & (1 << 7),
            Ps2InputType.L2 when digital => ~ps2Data[4] & (1 << 0),
            Ps2InputType.R2 when digital => ~ps2Data[4] & (1 << 1),
            Ps2InputType.L1 when digital => ~ps2Data[4] & (1 << 2),
            Ps2InputType.R1 when digital => ~ps2Data[4] & (1 << 3),
            Ps2InputType.Triangle when digital => ~ps2Data[4] & (1 << 4),
            Ps2InputType.Circle when digital => ~ps2Data[4] & (1 << 5),
            Ps2InputType.Cross when digital => ~ps2Data[4] & (1 << 6),
            Ps2InputType.Square when digital => ~ps2Data[4] & (1 << 7),
            _ => RawValue
        };
    }

    public bool SupportsType(Ps2ControllerType type)
    {
        var types = new List<Ps2ControllerType>();
        if (AxisToType.TryGetValue(Input, out var value))
        {
            types.Add(value);
        }
        else if (Dualshock2Order.Contains(Input))
        {
            types.Add(Ps2ControllerType.Dualshock2);
        }
        else if (DigitalButtons.Contains(Input))
        {
            types.Add(Ps2ControllerType.Digital);
            types.Add(Ps2ControllerType.Dualshock);
            types.Add(Ps2ControllerType.Dualshock2);
            types.Add(Ps2ControllerType.FlightStick);
        }

        if (GuitarButtons.Contains(Input)) types.Add(Ps2ControllerType.Guitar);

        if (Dualshock.Contains(Input))
        {
            types.Add(Ps2ControllerType.Dualshock);
            types.Add(Ps2ControllerType.FlightStick);
        }

        return types.Contains(type);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        Dictionary<Ps2InputType, string> ds2Axis = new();
        Dictionary<Ps2ControllerType, List<string>> mappedBindings = new();
        foreach (var binding in bindings)
            if (binding.Item1.InnermostInput() is Ps2Input input)
            {
                var types = new List<Ps2ControllerType>();
                if (AxisToType.TryGetValue(input.Input, out var value))
                {
                    types.Add(value);
                }
                else if (Dualshock2Order.Contains(input.Input))
                {
                    ds2Axis[input.Input] = binding.Item2;
                }
                else if (DigitalButtons.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Digital);
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.Dualshock2);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                if (GuitarButtons.Contains(input.Input)) types.Add(Ps2ControllerType.Guitar);

                if (Dualshock.Contains(input.Input))
                {
                    types.Add(Ps2ControllerType.Dualshock);
                    types.Add(Ps2ControllerType.FlightStick);
                }

                foreach (var type in types)
                {
                    if (!mappedBindings.ContainsKey(type)) mappedBindings.Add(type, new List<string>());

                    mappedBindings[type].Add(binding.Item2);
                }
            }

        var i = 0;
        var retDs2 = "";
        foreach (var binding in Dualshock2Order)
            if (ds2Axis.TryGetValue(binding, out var axi))
            {
                retDs2 += axi.Replace("generated", i.ToString()) + "\n";
                i++;
            }

        if (!string.IsNullOrEmpty(retDs2))
        {
            var mappings = mappedBindings.GetValueOrDefault(Ps2ControllerType.Dualshock2, new List<string>());
            mappings.Add(retDs2);
            mappedBindings[Ps2ControllerType.Dualshock2] = mappings;
        }

        var ret = "";
        foreach (var (input, mappings) in mappedBindings)
            ret += @$"case {CType[input]}:
                        {string.Join("\n", mappings)};
                        break;";

        if (ret == "") return "";
        return @$"if (ps2Valid) {{ 
                    switch (ps2ControllerType) {{
                        {ret}
                    }}
                 }}
        ";
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        var defines = base.RequiredDefines().ToList();
        defines.Add("INPUT_PS2");
        defines.Add($"PS2_ACK {Ack}");
        defines.Add($"INPUT_PS2_ATT_SET() {Model.Microcontroller.GenerateDigitalWrite(Att, true)}");
        defines.Add($"INPUT_PS2_ATT_CLEAR() {Model.Microcontroller.GenerateDigitalWrite(Att, false)}");
        var ack = Model.Microcontroller.GenerateAckDefines(Ack);
        if (!string.IsNullOrEmpty(ack)) defines.Add(ack);

        return defines;
    }
}