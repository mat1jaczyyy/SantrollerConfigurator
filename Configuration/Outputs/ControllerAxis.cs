using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
public class ControllerAxis : OutputAxis
{
    
    public static readonly Dictionary<StandardAxisType, string> Mappings = new() {
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
    
    public static readonly Dictionary<StandardAxisType, string> MappingsXbox = new() {
        {StandardAxisType.LeftStickX, "l_x"},
        {StandardAxisType.LeftStickY, "l_y"},
        {StandardAxisType.RightStickX, "r_x"},
        {StandardAxisType.RightStickY, "r_y"},
        {StandardAxisType.LeftTrigger, "lt"},
        {StandardAxisType.RightTrigger, "rt"},
    };
    public ControllerAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, float multiplier, int offset, int deadzone, StandardAxisType type) : base(model, input, ledOn, ledOff, multiplier, offset, deadzone, type.ToString())
    {
        Type = type;
    }
    public StandardAxisType Type { get; }
    public override bool Trigger => (Model.DeviceType is DeviceControllerType.Guitar && Type is StandardAxisType.RightStickX) || Type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;

    public override string GenerateOutput(bool xbox)
    {
        if (xbox)
        {
            return "report->" + MappingsXbox[Type];
        }
        return "report->" + Mappings[Type];
    }
    public override bool IsCombined => false;
    public override SerializedOutput GetJson()
    {
        return new SerializedControllerAxis(Input?.GetJson(), Type, LedOn, LedOff, Multiplier, Offset, Deadzone);
    }
}