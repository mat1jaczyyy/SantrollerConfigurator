using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputAxis : Output
{
    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, float multiplier, int offset,
        int deadZone, string name) : base(model, input, ledOn, ledOff, name)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        DeadZone = deadZone;
    }

    public float Multiplier { get; set; }
    public int Offset { get; set; }
    public int DeadZone { get; set; }
    public abstract bool Trigger { get; }

    public abstract string GenerateOutput(bool xbox);
    public override bool IsCombined => false;

    public override string Generate(bool xbox, int debounceIndex)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        bool isUInt = Input.InnermostInput()?.IsUint == true;
        string function;
        if (xbox)
        {
            function = Trigger ? "handle_calibration_xbox_trigger" : "handle_calibration_xbox";
        }
        else
        {
            function = Trigger ? "handle_calibration_ps3_trigger" : "handle_calibration_ps3"; 
        }

        if (isUInt)
        {
            function += "_uint";
        }
        else
        {
            function += "_int";
        }

        int mulInt = (int) (Multiplier * 1024);
        if (mulInt > ushort.MaxValue)
        {
            mulInt = ushort.MaxValue;
        }
        return
            $"{GenerateOutput(xbox)} = {function}({Input.Generate()}, {Offset}, {mulInt}, {DeadZone})";
    }
}