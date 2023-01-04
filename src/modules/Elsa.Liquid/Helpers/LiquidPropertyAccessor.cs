using Fluid.Values;

namespace Elsa.Liquid.Helpers;

/// <summary>
/// Can be used to provide a factory to return a value based on a property name 
/// that is unknown at registration time. 
/// 
/// e.g. {{ LiquidPropertyAccessor.MyPropertyName }} (MyPropertyName will be passed as the identifier argument to the factory)
/// </summary>
public class LiquidPropertyAccessor : LiquidObjectAccessor<FluidValue>
{
    public LiquidPropertyAccessor(Func<string, Task<FluidValue>> getter) : base(getter!)
    {
    }
}