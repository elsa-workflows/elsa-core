using Elsa.Liquid.Contracts;
using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Filters;

/// <summary>
/// A liquid filter that extracts the key values from a <see cref="DictionaryValue"/> value.
/// </summary>
public class DictionaryKeysFilter : ILiquidFilter
{
    /// <inheritdoc />
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.ToObjectValue() is not IFluidIndexable dictionary)
            throw new ArgumentOutOfRangeException($"This filter only works on objects of type {typeof(IFluidIndexable)}");

        var keys = dictionary.Keys.Select(x => new StringValue(x)).Cast<FluidValue>().ToArray();
        return new ValueTask<FluidValue>(new ArrayValue(keys));
    }
}