using System;
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class Input: ReactiveObject
{
    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate();

    public abstract JsonInput GetJson();

    public abstract bool IsAnalog { get; }

    public virtual Input InnermostInput()
    {
        return this;
    }

    public abstract string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller);
}