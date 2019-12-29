using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Workflows.Activities;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Activities.Workflows.Extensions
{
    public static class WorkflowInvokerExtensions
    {
        public static async Task TriggerSignalAsync(
            this IProcessRunner processRunner,
            string signalName,
            Func<Variables, bool> activityStatePredicate = null,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            await processRunner.TriggerAsync(nameof(Signaled), Variable.From(signalName), correlationId, activityStatePredicate, cancellationToken);
        }
    }
}