using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchWorkflowRequestHandler :
    ICommandHandler<DispatchTriggerWorkflowsCommand>,
    ICommandHandler<DispatchWorkflowDefinitionCommand>,
    ICommandHandler<DispatchWorkflowInstanceCommand>,
    ICommandHandler<DispatchResumeWorkflowsCommand>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowRequestHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions(command.CorrelationId, command.WorkflowInstanceId, command.ActivityInstanceId, command.Input);
        await _workflowRuntime.TriggerWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options, cancellationToken);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowRuntimeOptions(command.CorrelationId, command.Input, command.VersionOptions, InstanceId: command.InstanceId, TriggerActivityId: command.TriggerActivityId);

        await _workflowRuntime.TryStartWorkflowAsync(command.DefinitionId, options, cancellationToken);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowRuntimeOptions(
            command.CorrelationId,
            command.BookmarkId,
            command.ActivityId,
            command.ActivityNodeId,
            command.ActivityInstanceId,
            command.ActivityHash,
            command.Input);

        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options, cancellationToken);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions(CorrelationId: command.CorrelationId, Input: command.Input, WorkflowInstanceId: command.WorkflowInstanceId, ActivityInstanceId: command.ActivityInstanceId);
        await _workflowRuntime.ResumeWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options, cancellationToken);

        return Unit.Instance;
    }
}