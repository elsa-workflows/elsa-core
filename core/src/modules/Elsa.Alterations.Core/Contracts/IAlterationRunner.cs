using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Runs a series of alterations on the specified workflow execution context.
/// </summary>
public interface IAlterationRunner
{
    /// <summary>
    /// Runs a series of alterations on the specified workflow instances.
    /// </summary>
    /// <param name="workflowInstanceIds">The IDs of the workflow instances to alter.</param>
    /// <param name="alterations">The alterations to run.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<ICollection<RunAlterationsResult>> RunAsync(IEnumerable<string> workflowInstanceIds, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Runs a series of alterations on the specified workflow instances.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to alter.</param>
    /// <param name="alterations">The alterations to run.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<RunAlterationsResult> RunAsync(string workflowInstanceId, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default);
}