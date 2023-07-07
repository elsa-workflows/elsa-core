using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowActivationStrategies.Contracts;

/// <summary>
/// Represents a client for the variable types API.
/// </summary>
public interface IWorkflowActivationStrategiesApi
{
    /// <summary>
    /// Lists workflow activation strategies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/descriptors/workflow-activation-strategies")]
    Task<ListResponse<WorkflowActivationStrategyDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}