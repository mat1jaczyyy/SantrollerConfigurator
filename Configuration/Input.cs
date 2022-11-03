using System;
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class Input : ReactiveObject, IDisposable
{
    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate();

    public abstract SerializedInput GetJson();

    public abstract bool IsAnalog { get; }
    public abstract bool IsUint { get; }

    public virtual Input InnermostInput()
    {
        return this;
    }

    public abstract List<DevicePin> Pins { get; }
    public abstract InputType? InputType { get; }

    public abstract string GenerateAll(List<Tuple<Input, string>> bindings);

    public abstract void Dispose();
}