using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoInclude(100, typeof(SerializedKeyboardButton))]
[ProtoInclude(101, typeof(SerializedMouseAxis))]
[ProtoInclude(102, typeof(SerializedMouseButton))]
[ProtoInclude(103, typeof(SerializedControllerAxis))]
[ProtoInclude(104, typeof(SerializedControllerButton))]
[ProtoInclude(110, typeof(SerializedDrumAxis))]

[ProtoInclude(105, typeof(SerializedDjCombinedOutput))]
[ProtoInclude(106, typeof(SerializedGh5CombinedOutput))]
[ProtoInclude(107, typeof(SerializedGhwtCombinedOutput))]
[ProtoInclude(108, typeof(SerializedPs2CombinedOutput))]
[ProtoInclude(109, typeof(SerializedWiiCombinedOutput))]

[ProtoContract]
public abstract class SerializedOutput
{
    public abstract SerializedInput? Input { get; }
    public abstract uint LedOn { get; }
    public abstract uint LedOff { get; }
    public abstract byte[] LedIndex { get; }
    public abstract Output Generate(ConfigViewModel model, Microcontroller microcontroller);
}