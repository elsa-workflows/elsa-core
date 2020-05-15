using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    public class FaultWorkflowResult : ActivityExecutionResult
    {
        private readonly string errorMessage;
        private Exception exception;

        public FaultWorkflowResult(Exception exception) : this(exception.Message)
        {
            this.exception = exception;
        }

        public FaultWorkflowResult(string errorMessage)
        {
            this.errorMessage = errorMessage;
        }

        public override async Task ExecuteAsync(
            IWorkflowInvoker invoker,
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var eventHandlers = workflowContext.ServiceProvider.GetServices<IWorkflowEventHandler>();
            var logger = workflowContext.ServiceProvider.GetRequiredService<ILogger<FaultWorkflowResult>>();
            var currentActivity = workflowContext.CurrentActivity;
            
            await eventHandlers.InvokeAsync(
                x => x.ActivityFaultedAsync(workflowContext, currentActivity, errorMessage, cancellationToken),
                logger);

            workflowContext.Fault(workflowContext.CurrentActivity, errorMessage, exception);
        }
    }
}