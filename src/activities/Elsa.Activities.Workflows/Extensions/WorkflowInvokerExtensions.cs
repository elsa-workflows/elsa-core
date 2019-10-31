using System;
using System.Collections.Generic;
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
            this IWorkflowInvoker workflowInvoker,
            string signalName,
            Variables input = default,
            Func<JObject, bool> activityStatePredicate = null,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var combinedInput = new Variables(
                new Dictionary<string, object>
                {
                    ["Signal"] = signalName
                });

            if (input != null)
                combinedInput.AddVariables(input);

            await workflowInvoker.TriggerAsync(nameof(Signaled), combinedInput, correlationId, activityStatePredicate, cancellationToken);
        }
    }
}