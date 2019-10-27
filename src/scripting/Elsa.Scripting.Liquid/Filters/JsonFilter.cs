﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Services;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json;

namespace Elsa.Scripting.Liquid.Filters
{
    public class JsonFilter : ILiquidFilter
    {
        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            switch (input.Type)
            {
                case FluidValues.Array:
                    return new ValueTask<FluidValue>(new StringValue(JsonConvert.SerializeObject(input.Enumerate().Select(o => o.ToObjectValue()))));

                case FluidValues.Boolean:
                    return new ValueTask<FluidValue>(new StringValue(JsonConvert.SerializeObject(input.ToBooleanValue())));

                case FluidValues.Nil:
                    return new ValueTask<FluidValue>(FluidValue.Create("null"));

                case FluidValues.Number:
                    return new ValueTask<FluidValue>(new StringValue(JsonConvert.SerializeObject(input.ToNumberValue())));

                case FluidValues.DateTime:
                case FluidValues.Dictionary:
                case FluidValues.Object:
                    return new ValueTask<FluidValue>(new StringValue(JsonConvert.SerializeObject(input.ToObjectValue())));

                case FluidValues.String:
                    var stringValue = input.ToStringValue();

                    return string.IsNullOrWhiteSpace(stringValue)
                        ? new ValueTask<FluidValue>(input)
                        : new ValueTask<FluidValue>(new StringValue(JsonConvert.SerializeObject(stringValue)));
            }

            throw new NotSupportedException("Unrecognized FluidValue");
        }
    }
}