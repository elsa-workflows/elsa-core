using Elsa.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Reports whether the workflow runtime can create clients and is currently accepting new work.
/// </summary>
public class ElsaRuntimeHealthCheck(IWorkflowRuntime workflowRuntime, IQuiescenceSignal quiescenceSignal, ILogger<ElsaRuntimeHealthCheck> logger) : IHealthCheck
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (!e.IsFatal())
        {
            logger.LogWarning(e, "Elsa workflow runtime could not create a workflow client.");
            return HealthCheckResult.Unhealthy("Elsa workflow runtime could not create a workflow client.", data: new Dictionary<string, object>
            {
                ["category"] = "runtime"
            });
        }
    }
}
