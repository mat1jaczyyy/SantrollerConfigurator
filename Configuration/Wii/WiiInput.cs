using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Wii;

public interface IWiiInput {
    public WiiController WiiController { get; }
    static public Dictionary<WiiController, string> caseStatements = new Dictionary<WiiController, string>() {
        {WiiController.ClassicController, "WII_CLASSIC_CONTROLLER"}
    };
}