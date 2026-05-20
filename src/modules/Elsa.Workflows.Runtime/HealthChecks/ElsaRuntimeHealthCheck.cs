using Elsa.Common;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Reports whether the workflow runtime can create clients and is currently accepting new work.
/// </summary>
public class ElsaRuntimeHealthCheck(IWorkflowRuntime workflowRuntime, IQuiescenceSignal quiescenceSignal) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowRuntime.CreateClientAsync(cancellationToken);

            var state = quiescenceSignal.CurrentState;
            var data = new Dictionary<string, object>
            {
                ["category"] = "runtime",
                ["acceptingNewWork"] = state.IsAcceptingNewWork,
                ["reason"] = state.Reason.ToString(),
                ["activeExecutionCycles"] = quiescenceSignal.ActiveExecutionCycleCount
            };

            return state.IsAcceptingNewWork
                ? HealthCheckResult.Healthy("Elsa workflow runtime is ready.", data)
                : HealthCheckResult.Degraded("Elsa workflow runtime is paused or draining.", data: data);
        }
        catch (Exception e) when (!e.IsFatal())
        {
            return HealthCheckResult.Unhealthy("Elsa workflow runtime could not create a workflow client.", e, new Dictionary<string, object>
            {
                ["category"] = "runtime"
            });
        }
    }
}
