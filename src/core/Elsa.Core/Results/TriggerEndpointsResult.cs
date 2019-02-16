using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class TriggerEndpointsResult : ActivityExecutionResult
    {
        public TriggerEndpointsResult(IEnumerable<string> endpointNames)
        {
            EndpointNames = endpointNames.ToList();
        }

        public IReadOnlyList<string> EndpointNames { get; }

        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var currentActivity = workflowContext.CurrentActivity;

            foreach (var endpointName in EndpointNames)
            {
                workflowContext.ScheduleNextActivities(workflowContext, new SourceEndpoint(currentActivity, endpointName));
            }

            var eventHandlers = workflowContext.ServiceProvider.GetServices<IWorkflowEventHandler>();
            var logger = workflowContext.ServiceProvider.GetRequiredService<ILogger<TriggerEndpointsResult>>();
            await eventHandlers.InvokeAsync(x => x.ActivityExecutedAsync(workflowContext, currentActivity, cancellationToken), logger);
        }
    }
}