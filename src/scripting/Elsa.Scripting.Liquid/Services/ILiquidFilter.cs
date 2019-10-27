using System.Threading.Tasks;
using Fluid;
using Fluid.Values;

namespace Elsa.Scripting.Liquid.Services
{
    public interface ILiquidFilter
    {
        ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext ctx);
    }
}
