using System.Threading.Tasks;
using Elsa.Activities.Signaling.Extensions;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;

namespace Elsa.Scripting.Liquid.Filters
{
    public class SignalTokenFilter : ILiquidFilter
    {
        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var activityExecutionContext = (ActivityExecutionContext) context.Model.ToObjectValue();
            var signalName = input.ToStringValue();
            var token = activityExecutionContext.GenerateSignalToken(signalName);
            return new ValueTask<FluidValue>(new StringValue(token));
        }
    }
}