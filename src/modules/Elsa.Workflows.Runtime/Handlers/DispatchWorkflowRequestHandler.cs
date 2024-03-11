using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchWorkflowCommandHandler(IWorkflowRuntime workflowRuntime) :
    ICommandHandler<DispatchTriggerWorkflowsCommand>,
    ICommandHandler<DispatchWorkflowDefinitionCommand>,
    ICommandHandler<DispatchWorkflowInstanceCommand>,
    ICommandHandler<DispatchResumeWorkflowsCommand>
{
    public virtual async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = command.CorrelationId,
            WorkflowInstanceId = command.WorkflowInstanceId,
            ActivityInstanceId = command.ActivityInstanceId,
            Input = command.Input,
            Properties = command.Properties,
            CancellationTokens = cancellationToken
        };
        await workflowRuntime.TriggerWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowRuntimeParams
        {
            CorrelationId = command.CorrelationId,
            Input = command.Input,
            Properties = command.Properties,
            VersionOptions = command.VersionOptions,
            InstanceId = command.InstanceId,
            TriggerActivityId = command.TriggerActivityId,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.TryStartWorkflowAsync(command.DefinitionId, options);

        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowRuntimeParams
        {
            CorrelationId = command.CorrelationId,
            BookmarkId = command.BookmarkId,
            ActivityId = command.ActivityId,
            ActivityNodeId = command.ActivityNodeId,
            ActivityInstanceId = command.ActivityInstanceId,
            ActivityHash = command.ActivityHash,
            Input = command.Input,
            Properties = command.Properties,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options);

        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = command.CorrelationId,
            Input = command.Input,
            Properties = command.Properties,
            WorkflowInstanceId = command.WorkflowInstanceId,
            ActivityInstanceId = command.ActivityInstanceId,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.ResumeWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }
}