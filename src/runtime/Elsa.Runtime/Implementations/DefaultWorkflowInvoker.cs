using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
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
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowRegistry _workflowRegistry;

    public DefaultWorkflowInvoker(IWorkflowInstanceFactory workflowInstanceFactory, IRequestSender requestSender, IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry)
    {
        _workflowInstanceFactory = workflowInstanceFactory;
        _requestSender = requestSender;
        _workflowRunner = workflowRunner;
        _workflowRegistry = workflowRegistry;
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await _workflowInstanceFactory.CreateAsync(request.DefinitionId, request.VersionOptions, request.CorrelationId, cancellationToken);
        return await InvokeAsync(workflowInstance!, input: request.Input, cancellationToken: cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await _requestSender.RequestAsync(new FindWorkflowInstance(request.InstanceId), cancellationToken);
        return await InvokeAsync(workflowInstance!, request.Bookmark, request.Input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRegistry.FindByIdAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

        if (workflow == null)
            throw new Exception($"Workflow instance references a workflow that does not exist");

        var workflowState = workflowInstance.WorkflowState;
        return await InvokeAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        return await _workflowRunner.RunAsync(workflow, workflowState, bookmark, input, cancellationToken);
    }
}