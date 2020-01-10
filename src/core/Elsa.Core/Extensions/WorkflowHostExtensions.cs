using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling;
using Elsa.Models;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class WorkflowHostExtensions
    {
        public static async Task TriggerSignalAsync(
            this IWorkflowHost workflowHost,
            string signalName,
            Func<Variables, bool>? activityStatePredicate = null,
            string? correlationId = default,
            CancellationToken cancellationToken = default) =>
            await workflowHost.TriggerAsync(nameof(Signaled), signalName, correlationId, activityStatePredicate, cancellationToken);
    }
}