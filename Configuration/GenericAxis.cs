using System.Collections.Generic;
using Dahomey.Json.Attributes;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

[JsonDiscriminator(nameof(GenericAxis))]
public class GenericAxis : IOutputAxis
{
    public StandardAxisType Type { get; }
    public OutputType OutputType => OutputType.Generic;

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

    public string Generate(bool xbox)
    {
        if (xbox)
        {
            return "report->" + MappingsXbox[Type];
        }
        return "report->" + Mappings[Type];
    }

    public GenericAxis(StandardAxisType type)
    {
        Type = type;
    }
}