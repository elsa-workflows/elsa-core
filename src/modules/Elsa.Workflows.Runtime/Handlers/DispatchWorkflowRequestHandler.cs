using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;

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
        var options = new TriggerWorkflowsOptions(command.CorrelationId, command.WorkflowInstanceId, command.ActivityInstanceId, command.Input, cancellationToken);
        await _workflowRuntime.TriggerWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowRuntimeOptions(
            command.CorrelationId, 
            command.Input, 
            command.VersionOptions, 
            InstanceId: command.InstanceId, 
            TriggerActivityId: command.TriggerActivityId, 
            CancellationTokens: cancellationToken);

        await _workflowRuntime.TryStartWorkflowAsync(command.DefinitionId, options);

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
            command.Input,
            cancellationToken);

        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions(
            correlationId: command.CorrelationId, 
            input: command.Input, 
            workflowInstanceId: command.WorkflowInstanceId, 
            activityInstanceId: command.ActivityInstanceId,
            cancellationTokens: cancellationToken);
        
        await _workflowRuntime.ResumeWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }
}