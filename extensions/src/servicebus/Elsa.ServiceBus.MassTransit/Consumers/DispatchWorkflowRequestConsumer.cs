using Elsa.ServiceBus.MassTransit.Messages;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.ServiceBus.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
[UsedImplicitly]
public class DispatchWorkflowRequestConsumer(IWorkflowDefinitionService workflowDefinitionService, IWorkflowRuntime workflowRuntime, ILogger<DispatchWorkflowRequestConsumer> logger) :
    IConsumer<DispatchWorkflowDefinition>,
    IConsumer<DispatchWorkflowInstance>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowRequestConsumer"/> class without a logger.
    /// </summary>
    [Obsolete("Use the constructor that takes an ILogger instead. Removed in the next major version.")]
    public DispatchWorkflowRequestConsumer(IWorkflowDefinitionService workflowDefinitionService, IWorkflowRuntime workflowRuntime)
        : this(workflowDefinitionService, workflowRuntime, NullLogger<DispatchWorkflowRequestConsumer>.Instance)
    {
    }

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

        // Same rule as the default dispatcher: a redelivered message must not create a second instance with the same ID.
        if (message.SkipIfInstanceExists && !string.IsNullOrWhiteSpace(message.InstanceId) && await workflowClient.InstanceExistsAsync(cancellationToken))
        {
            return;
        }

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

        var workflowClient = await workflowRuntime.CreateClientAsync(message.InstanceId, cancellationToken);
        var request = new RunWorkflowInstanceRequest
        {
            TriggerActivityId = message.TriggerActivityId,
            Input = message.Input,
            Properties = message.Properties
        };

        // The pending guard is decided by the client under the instance lock (#8869): reading
        // the sub-status here would be stale by the time the run takes the lock.
        var response = await workflowClient.RunInstanceIfPendingAsync(request, cancellationToken);
        if (response == null)
            logger.LogDebug("Skipping existing-instance dispatch of {InstanceId}: it is no longer pending.", message.InstanceId);
    }
}