using Elsa.MassTransit.Messages;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
[UsedImplicitly]
public class DispatchWorkflowRequestConsumer(IWorkflowDefinitionService workflowDefinitionService, IWorkflowRuntime workflowRuntime, IStimulusSender stimulusSender) :
    IConsumer<DispatchWorkflowDefinition>,
    IConsumer<DispatchWorkflowInstance>,
    IConsumer<DispatchTriggerWorkflows>,
    IConsumer<DispatchResumeWorkflows>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowDefinition> context)
    {
        if (context.Message.IsExistingInstance)
            await DispatchExistingWorkflowInstanceAsync(context.Message, context.CancellationToken);
        else
            await DispatchNewWorkflowInstanceAsync(context.Message, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowInstance> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var request = new RunWorkflowInstanceRequest
        {
            BookmarkId = message.BookmarkId,
            ActivityHandle = message.ActivityHandle,
            Input = message.Input,
            Properties = message.Properties
        };
        var workflowClient = await workflowRuntime.CreateClientAsync(message.InstanceId, cancellationToken);
        await workflowClient.RunInstanceAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchTriggerWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var options = new StimulusMetadata
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            ActivityInstanceId = message.ActivityInstanceId,
            Input = message.Input,
            Properties = message.Properties
        };

        var stimulus = message.BookmarkPayload;
        await stimulusSender.SendAsync(message.ActivityTypeName, stimulus, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchResumeWorkflows> context)
    {
        var message = context.Message;

        var stimulusMetadata = new StimulusMetadata
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            Input = message.Input,
            Properties = message.Properties
        };

        var stimulus = message.BookmarkPayload;
        await stimulusSender.SendAsync(message.ActivityTypeName, stimulus, stimulusMetadata, context.CancellationToken);
    }

    private async Task DispatchNewWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        var definitionId = message.DefinitionId;
        var versionOptions = message.VersionOptions;
        var definitionVersionId = message.DefinitionVersionId;
        if (definitionId == null && definitionVersionId == null) throw new ArgumentException("The definition ID is required when dispatching a workflow definition.");
        if (versionOptions == null && definitionVersionId == null) throw new ArgumentException("The version options are required when dispatching a workflow definition.");

        var workflowGraph = definitionVersionId != null 
            ? await workflowDefinitionService.FindWorkflowGraphAsync(definitionVersionId, cancellationToken)
            : await workflowDefinitionService.FindWorkflowGraphAsync(definitionId!, versionOptions!.Value, cancellationToken);
        
        if (workflowGraph == null)
            throw new Exception($"Workflow definition version with ID '{definitionVersionId}' not found");

        var workflowClient = await workflowRuntime.CreateClientAsync(message.InstanceId, cancellationToken);
        var createWorkflowInstanceRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = workflowGraph.Workflow.DefinitionHandle,
            Properties = message.Properties,
            CorrelationId = message.CorrelationId,
            Input = message.Input,
            ParentId = message.ParentWorkflowInstanceId,
            TriggerActivityId = message.TriggerActivityId
        };
        await workflowClient.CreateAndRunInstanceAsync(createWorkflowInstanceRequest, cancellationToken);
    }

    private async Task DispatchExistingWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId)) throw new ArgumentException("The instance ID is required when dispatching an existing workflow instance.");

        var request = new RunWorkflowInstanceRequest
        {
            TriggerActivityId = message.TriggerActivityId,
            Input = message.Input,
            Properties = message.Properties
        };

        var workflowClient = await workflowRuntime.CreateClientAsync(message.InstanceId, cancellationToken);
        await workflowClient.RunInstanceAsync(request, cancellationToken);
    }
}