using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Runtime.Contracts;

/// <summary>
/// Represents a high-level service to invoke workflows.
/// </summary>
public interface IWorkflowService
{
    Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken);
    Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken);
        
}