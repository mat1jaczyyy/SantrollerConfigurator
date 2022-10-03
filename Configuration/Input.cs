using System;
using System.Collections.Generic;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class Input: ReactiveObject
{
    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate();

    public abstract bool IsAnalog { get; }

    public virtual Input InnermostInput()
    {
        return this;
    }

    public abstract string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller.Microcontroller controller);
}