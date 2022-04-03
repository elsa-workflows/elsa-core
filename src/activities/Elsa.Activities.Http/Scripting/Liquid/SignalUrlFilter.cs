using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;

namespace Elsa.Activities.Http.Scripting.Liquid
{
    public class SignalUrlFilter : ILiquidFilter
    {
        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var activityExecutionContext = (ActivityExecutionContext) context.Model.ToObjectValue();
            var signalName = input.ToStringValue();
            var url = activityExecutionContext.GenerateSignalUrl(signalName);
            return new ValueTask<FluidValue>(new StringValue(url));
        }
    }
}