using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Services
{
    public interface ILiquidFilter
    {
        ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext ctx);
    }
}
