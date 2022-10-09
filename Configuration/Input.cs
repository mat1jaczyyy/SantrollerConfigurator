using System;
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class Input: ReactiveObject, IDisposable
{
    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate();

    public abstract SerializedInput GetJson();

    public abstract bool IsAnalog { get; }

    public virtual Input InnermostInput()
    {
        return this;
    }

    public abstract List<DevicePin> Pins { get; }

    public abstract string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller);

    public abstract void Dispose();
}