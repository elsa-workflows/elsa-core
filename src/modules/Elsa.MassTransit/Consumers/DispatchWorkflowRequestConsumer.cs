using Elsa.MassTransit.Messages;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
[UsedImplicitly]
public class DispatchWorkflowRequestConsumer(IWorkflowRuntime workflowRuntime, IPayloadSerializer jsonSerializer) :
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
        var input = message.Input ?? DeserializeInput(message.SerializedInput);

        var options = new ResumeWorkflowRuntimeParams
        {
            CorrelationId = message.CorrelationId,
            BookmarkId = message.BookmarkId,
            ActivityId = message.ActivityId,
            ActivityNodeId = message.ActivityNodeId,
            ActivityInstanceId = message.ActivityInstanceId,
            ActivityHash = message.ActivityHash,
            Input = input,
            Properties = message.Properties,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.ResumeWorkflowAsync(message.InstanceId, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchTriggerWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var input = message.Input ?? DeserializeInput(message.SerializedInput);
        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            ActivityInstanceId = message.ActivityInstanceId,
            Input = input,
            Properties = message.Properties,
            CancellationTokens = cancellationToken
        };
        await workflowRuntime.TriggerWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchResumeWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var input = message.Input ?? DeserializeInput(message.SerializedInput);

        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            Input = input,
            Properties = message.Properties,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.ResumeWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }

    private async Task DispatchNewWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.DefinitionId)) throw new ArgumentException("The definition ID is required when dispatching a workflow definition.");
        if (message.VersionOptions == null) throw new ArgumentException("The version options are required when dispatching a workflow definition.");
        var input = message.Input ?? DeserializeInput(message.SerializedInput);

        var options = new StartWorkflowRuntimeParams
        {
            ParentWorkflowInstanceId = message.ParentWorkflowInstanceId,
            CorrelationId = message.CorrelationId,
            Input = input,
            Properties = message.Properties,
            VersionOptions = message.VersionOptions.Value,
            TriggerActivityId = message.TriggerActivityId,
            InstanceId = message.InstanceId,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.TryStartWorkflowAsync(message.DefinitionId, options);
    }

    private async Task DispatchExistingWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId)) throw new ArgumentException("The instance ID is required when dispatching an existing workflow instance.");

        var options = new StartWorkflowRuntimeParams
        {
            TriggerActivityId = message.TriggerActivityId,
            IsExistingInstance = true,
            InstanceId = message.InstanceId,
            CancellationTokens = cancellationToken
        };

        await workflowRuntime.StartWorkflowAsync(message.InstanceId, options);
    }

    private IDictionary<string, object>? DeserializeInput(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return jsonSerializer.Deserialize<IDictionary<string, object>>(json);
    }
}