using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Messages;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
[UsedImplicitly]
internal class DispatchWorkflowCommandHandler(IStimulusSender stimulusSender, IWorkflowRuntime workflowRuntime) :
    ICommandHandler<DispatchTriggerWorkflowsCommand>,
    ICommandHandler<DispatchWorkflowDefinitionCommand>,
    ICommandHandler<DispatchWorkflowInstanceCommand>,
    ICommandHandler<DispatchResumeWorkflowsCommand>
{
    public virtual async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var activityTypeName = command.ActivityTypeName;
        var stimulus = command.Stimulus;
        var metadata = new StimulusMetadata
        {
            CorrelationId = command.CorrelationId,
            ActivityInstanceId = command.ActivityInstanceId,
            WorkflowInstanceId = command.WorkflowInstanceId,
            Input = command.Input,
            Properties = command.Properties,
        };
        await stimulusSender.SendAsync(activityTypeName, stimulus, metadata, cancellationToken);

        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(command.InstanceId, cancellationToken);
        var createRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(command.DefinitionVersionId),
            CorrelationId = command.CorrelationId,
            Input = command.Input,
            Properties = command.Properties,
            ParentId = command.ParentWorkflowInstanceId,
            TriggerActivityId = command.TriggerActivityId
        };
        await client.CreateAndRunInstanceAsync(createRequest, cancellationToken);
        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var runRequest = new RunWorkflowInstanceRequest
        {
            BookmarkId = command.BookmarkId,
            ActivityHandle = command.ActivityHandle,
            Input = command.Input,
            Properties = command.Properties
        };
        var client = await workflowRuntime.CreateClientAsync(command.InstanceId, cancellationToken);
        await client.RunInstanceAsync(runRequest, cancellationToken);

        return Unit.Instance;
    }

    public virtual async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var activityTypeName = command.ActivityTypeName;
        var stimulus = command.Stimulus;
        var metadata = new StimulusMetadata
        {
            CorrelationId = command.CorrelationId,
            WorkflowInstanceId = command.WorkflowInstanceId,
            ActivityInstanceId = command.ActivityInstanceId,
            Properties = command.Properties,
            Input = command.Input
        };
        await stimulusSender.SendAsync(activityTypeName, stimulus, metadata, cancellationToken);

        return Unit.Instance;
    }
}