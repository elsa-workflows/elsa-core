using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a high-level service to invoke workflows.
/// </summary>
public interface IWorkflowService
{
    Task<InvokeWorkflowResult> ExecuteWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowDefinitionResponse> DispatchWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowInstanceResponse> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
    Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
        
}