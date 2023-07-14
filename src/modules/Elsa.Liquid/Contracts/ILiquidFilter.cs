using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Contracts;

/// <summary>
/// Represents a Liquid filter.
/// </summary>
public interface ILiquidFilter
{
    /// <summary>
    /// Processes the specified input.
    /// </summary>
    /// <param name="input">The input to process.</param>
    /// <param name="arguments">Any arguments passed to the filter.</param>
    /// <param name="context">The template context.</param>
    /// <returns>The processed input.</returns>
    ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context);
}