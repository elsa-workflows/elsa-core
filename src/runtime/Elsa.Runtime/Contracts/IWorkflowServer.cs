using Elsa.Models;

namespace Elsa.Runtime.Contracts;

public interface IWorkflowServer
{
    IServiceProvider ServiceProvider { get; }
    Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchWorkflowAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    Task<DispatchWorkflowResult> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken);
    Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken);
        
}