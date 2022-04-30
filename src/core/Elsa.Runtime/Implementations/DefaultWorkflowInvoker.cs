using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Persistence.Services;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;
using Elsa.Services;
using Elsa.State;

namespace Elsa.Runtime.Implementations;

/// <summary>
/// A basic implementation that directly executes the specified workflow in local memory (as opposed elsewhere in some cluster).
/// </summary>
public class DefaultWorkflowInvoker : IWorkflowInvoker
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowRegistry _workflowRegistry;

    public DefaultWorkflowInvoker(
        IWorkflowInstanceStore workflowInstanceStore, 
        IWorkflowRunner workflowRunner, 
        IWorkflowRegistry workflowRegistry)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _workflowRunner = workflowRunner;
        _workflowRegistry = workflowRegistry;
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(request.DefinitionId, request.VersionOptions, cancellationToken);
        var input = request.Input;
        return await _workflowRunner.RunAsync(workflow, input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await _workflowInstanceStore.FindByIdAsync(request.InstanceId, cancellationToken);
        return await InvokeAsync(workflowInstance!, request.Bookmark, request.Input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRegistry.FindByDefinitionIdAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
            throw new Exception($"Workflow instance references a workflow that does not exist");

        var workflowState = workflowInstance.WorkflowState;
        return await InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        return await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }
    
    private async Task<Workflow> GetWorkflowAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRegistry.FindByDefinitionIdAsync(definitionId, versionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception($"Workflow instance references a workflow that does not exist");

        return workflow;
    }
}