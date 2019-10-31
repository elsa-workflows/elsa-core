using System;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting.Liquid.Services;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;

namespace Elsa.Activities.Http.Liquid
{
    public class SignalUrlFilter : ILiquidFilter
    {
        private readonly ITokenService tokenService;
        private readonly IAbsoluteUrlProvider absoluteUrlProvider;

        public SignalUrlFilter(ITokenService tokenService, IAbsoluteUrlProvider absoluteUrlProvider)
        {
            this.tokenService = tokenService;
            this.absoluteUrlProvider = absoluteUrlProvider;
        }

        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var workflowContextValue = context.GetValue("WorkflowExecutionContext");

            if (workflowContextValue.IsNil())
                throw new ArgumentException("WorkflowExecutionContext missing while invoking 'signal_url'");

            var workflowContext = (WorkflowExecutionContext)workflowContextValue.ToObjectValue();
            var signalName = input.ToStringValue();
            var url = GenerateUrl(signalName, workflowContext);
            return new ValueTask<FluidValue>(new StringValue(url));
        }

        private string GenerateUrl(string signal, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;
            var payload = new Signal(signal, workflowInstanceId);
            var token = tokenService.CreateToken(payload);
            var url = $"/workflows/signal?token={token}";

            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}