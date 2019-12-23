using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Workflows.Activities;
using Elsa.Models;
using Elsa.Services;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Workflows.Extensions
{
    public static class WorkflowInvokerExtensions
    {
        public static async Task TriggerSignalAsync(
            this IWorkflowRunner workflowRunner,
            string signalName,
            Variables input = default,
            Func<Variables, bool> activityStatePredicate = null,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var combinedInput = new Variables();
            combinedInput.SetVariable("Signal", signalName);

            if (input != null)
                combinedInput.SetVariables(input);

            await workflowRunner.TriggerAsync(nameof(Signaled), combinedInput, correlationId, activityStatePredicate, cancellationToken);
        }
    }
}