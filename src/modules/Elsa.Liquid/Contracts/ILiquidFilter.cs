using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Contracts;

public interface ILiquidFilter
{
    ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext ctx);
}