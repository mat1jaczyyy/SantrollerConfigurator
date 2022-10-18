using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedWiiCombinedOutput : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input => null;
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Sda { get; }
    [ProtoMember(5)] public int Scl { get; }
    [ProtoMember(6)] public bool MapTapBarToFrets { get; set; }
    [ProtoMember(7)] public bool MapTapBarToAxis { get; set; }
    [ProtoMember(8)] public bool MapGuitarJoystickToDPad { get; set; }
    [ProtoMember(9)] public bool MapNunchukAccelerationToRightJoy { get; set; }

    public SerializedWiiCombinedOutput(Color ledOn, Color ledOff, int sda, int scl, bool mapTapBarToFrets,
        bool mapTapBarToAxis, bool mapGuitarJoystickToDPad, bool mapNunchukAccelerationToRightJoy)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Sda = sda;
        Scl = scl;
        MapTapBarToFrets = mapTapBarToFrets;
        MapTapBarToAxis = mapTapBarToAxis;
        MapGuitarJoystickToDPad = mapGuitarJoystickToDPad;
        MapNunchukAccelerationToRightJoy = mapNunchukAccelerationToRightJoy;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new WiiCombinedOutput(model, microcontroller, Sda, Scl, MapTapBarToFrets, MapTapBarToAxis,
            MapGuitarJoystickToDPad, MapNunchukAccelerationToRightJoy);
    }
}