using System.Globalization;
using System.Text;
using System.Text.Json;
using Elsa.Liquid.Contracts;
using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Filters;

/// <summary>
/// A liquid filter that converts a value into a base 64 string representation.
/// </summary>
public class Base64Filter : ILiquidFilter
{
    /// <inheritdoc />
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var text = InputToString(input, context);

        if (text == null)
            return new ValueTask<FluidValue>(NilValue.Instance);

        var bytes = Encoding.UTF8.GetBytes(text);
        var base64 = Convert.ToBase64String(bytes);

        return new ValueTask<FluidValue>(new StringValue(base64));
    }

    private static string? InputToString(FluidValue input, TemplateContext context)
    {
        return input.Type switch
        {
            FluidValues.Array => JsonSerializer.Serialize(input.Enumerate(context).Select(o => o.ToObjectValue())),
            FluidValues.Boolean => input.ToBooleanValue().ToString(),
            FluidValues.Nil => null,
            FluidValues.Number => input.ToNumberValue().ToString(CultureInfo.InvariantCulture),
            FluidValues.DateTime or FluidValues.Dictionary or FluidValues.Object => JsonSerializer.Serialize(input.ToObjectValue()),
            FluidValues.String => input.ToStringValue(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), "Unrecognized FluidValue")
        };
    }
}