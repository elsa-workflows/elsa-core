using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Services;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json;

namespace Elsa.Scripting.Liquid.Filters
{
    public class Base64Filter : ILiquidFilter
    {
        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var text = InputToString(input);

            if (text == null)
                return new ValueTask<FluidValue>(NilValue.Instance);

            var bytes = Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(bytes);

            return new ValueTask<FluidValue>(new StringValue(base64));
        }

        private string? InputToString(FluidValue input)
        {
            return input.Type switch
            {
                FluidValues.Array => JsonConvert.SerializeObject(input.Enumerate().Select(o => o.ToObjectValue())),
                FluidValues.Boolean => input.ToBooleanValue().ToString(),
                FluidValues.Nil => null,
                FluidValues.Number => input.ToNumberValue().ToString(CultureInfo.InvariantCulture),
                FluidValues.DateTime or FluidValues.Dictionary or FluidValues.Object => JsonConvert.SerializeObject(input.ToObjectValue()),
                FluidValues.String => input.ToStringValue(),
                _ => throw new ArgumentOutOfRangeException(nameof(input), "Unrecognized FluidValue")
            };
        }
    }
}