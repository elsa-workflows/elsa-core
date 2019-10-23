using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    public class FinishWorkflowResult : ActivityExecutionResult
    {
        public override async Task ExecuteAsync(
            IWorkflowInvoker invoker,
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var currentActivity = workflowContext.CurrentActivity;
            var eventHandlers = workflowContext.ServiceProvider.GetServices<IWorkflowEventHandler>();
            var logger = workflowContext.ServiceProvider.GetRequiredService<ILogger<OutcomeResult>>();
            await eventHandlers.InvokeAsync(
                x => x.ActivityExecutedAsync(workflowContext, currentActivity, cancellationToken),
                logger);

            workflowContext.Finish();
        }
    }
}