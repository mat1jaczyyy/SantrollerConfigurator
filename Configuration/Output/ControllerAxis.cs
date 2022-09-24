using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Output;

public class ControllerAxis : OutputAxis
{
    
    public static readonly Dictionary<StandardAxisType, string> Mappings = new Dictionary<StandardAxisType, string>() {
        {StandardAxisType.LeftStickX, "l_x"},
        {StandardAxisType.LeftStickY, "l_y"},
        {StandardAxisType.RightStickX, "r_x"},
        {StandardAxisType.RightStickY, "r_y"},
        {StandardAxisType.LeftTrigger, "axis[4]"},
        {StandardAxisType.RightTrigger, "axis[5]"},
        {StandardAxisType.AccelerationX, "accel[0]"},
        {StandardAxisType.AccelerationY, "accel[1]"},
        {StandardAxisType.AccelerationZ, "accel[2]"},
    };
    
    public static readonly Dictionary<StandardAxisType, string> MappingsXbox = new Dictionary<StandardAxisType, string>() {
        {StandardAxisType.LeftStickX, "l_x"},
        {StandardAxisType.LeftStickY, "l_y"},
        {StandardAxisType.RightStickX, "r_x"},
        {StandardAxisType.RightStickY, "r_y"},
        {StandardAxisType.LeftTrigger, "lt"},
        {StandardAxisType.RightTrigger, "rt"},
    };
    public ControllerAxis(IInput? input, Color ledOn, Color ledOff, float multiplier, int offset, int deadzone, bool trigger, StandardAxisType type) : base(input, ledOn, ledOff, multiplier, offset, deadzone, trigger)
    {
        Type = type;
    }
    public StandardAxisType Type { get; }
    public override string Name => Type.ToString();
    // TODO this
    public override string Image => Name;
    public override string GenerateOutput(bool xbox)
    {
        if (xbox)
        {
            return "report->" + MappingsXbox[Type];
        }
        return "report->" + Mappings[Type];
    }
}