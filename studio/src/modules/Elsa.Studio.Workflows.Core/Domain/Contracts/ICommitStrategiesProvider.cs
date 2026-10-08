using Elsa.Api.Client.Resources.CommitStrategies.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides commit strategies for workflows and activities.
/// </summary>
public interface ICommitStrategiesProvider
{
    /// <summary>
    /// Gets workflow commit strategies.
    /// </summary>
    ValueTask<IEnumerable<CommitStrategyDescriptor>> GetWorkflowCommitStrategiesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets activity commit strategies.
    /// </summary>
    ValueTask<IEnumerable<CommitStrategyDescriptor>> GetActivityCommitStrategiesAsync(CancellationToken cancellationToken = default);
}