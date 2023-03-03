using System.Text.Json;
using Elsa.Liquid.Contracts;
using Fluid;
using Fluid.Values;

namespace Elsa.Liquid.Filters;

/// <summary>
/// A liquid filter that converts a value into a JSON string representation.
/// </summary>
public class JsonFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        switch (input.Type)
        {
            case FluidValues.Array:
                return new ValueTask<FluidValue>(new StringValue(JsonSerializer.Serialize(input.Enumerate(context).Select(o => o.ToObjectValue()))));

            case FluidValues.Boolean:
                return new ValueTask<FluidValue>(new StringValue(JsonSerializer.Serialize(input.ToBooleanValue())));

            case FluidValues.Nil:
                return new ValueTask<FluidValue>(FluidValue.Create("null", context.Options));

            case FluidValues.Number:
                return new ValueTask<FluidValue>(new StringValue(JsonSerializer.Serialize(input.ToNumberValue())));

            case FluidValues.DateTime:
            case FluidValues.Dictionary:
            case FluidValues.Object:
                return new ValueTask<FluidValue>(new StringValue(JsonSerializer.Serialize(input.ToObjectValue())));

            case FluidValues.String:
                var stringValue = input.ToStringValue();

                return string.IsNullOrWhiteSpace(stringValue)
                    ? new ValueTask<FluidValue>(input)
                    : new ValueTask<FluidValue>(new StringValue(JsonSerializer.Serialize(stringValue)));
        }

        throw new NotSupportedException("Unrecognized FluidValue");
    }
}