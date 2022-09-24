using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarConfiguratorSharp.NetCore.Configuration.DJ;

public class DjInput : IInput
{
    public DjInputType InputType { get; set; }
    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        switch (InputType)
        {
            case DjInputType.LeftTurntable:
                return "((int8_t)dj_left[2]) << 5";
            case DjInputType.RightTurnable:
                return "((int8_t)dj_right[2]) << 5";
            case DjInputType.LeftAny:
                return "dj_left[0]";
            case DjInputType.RightAny:
                return "dj_right[0]";
            case DjInputType.LeftBlue:
            case DjInputType.LeftGreen:
            case DjInputType.LeftRed:
                return $"(dj_left[0] & {1 << ((byte) this.InputType) - ((byte) DjInputType.LeftGreen)})";
            case DjInputType.RightGreen:
            case DjInputType.RightRed:
            case DjInputType.RightBlue:
                return $"(dj_right[0] & {1 << ((byte) this.InputType) - ((byte) DjInputType.RightGreen)})";

        }

        throw new InvalidOperationException("Shouldn't get here!");
    }

    public bool IsAnalog()
    {
        return InputType <= DjInputType.RightTurnable;
    }

    public bool RequiresSpi()
    {
        return true;
    }

    public bool RequiresI2C()
    {
        return true;
    }

    public string GenerateAll(bool xbox, List<Tuple<IInput, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        return String.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DJ_TURNTABLE"};
    }
}

/*
#ifdef INPUT_DJ_TURNTABLE
    uint8_t dj_left[3] = {0,0,0};
    uint8_t dj_right[3] = {0,0,0};
    twi_readFromPointer(DJLEFT_ADDR, DJ_BUTTONS_PTR, sizeof(dj_left), dj_left);
    twi_readFromPointer(DJRIGHT_ADDR, DJ_BUTTONS_PTR, sizeof(dj_right), dj_right);
#endif

*/