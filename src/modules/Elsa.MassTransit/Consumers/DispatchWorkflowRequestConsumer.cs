using Elsa.MassTransit.Messages;
using Elsa.Workflows.Management.Contracts;
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
public class DispatchWorkflowRequestConsumer :
    IConsumer<DispatchWorkflowDefinition>,
    IConsumer<DispatchWorkflowInstance>,
    IConsumer<DispatchTriggerWorkflows>,
    IConsumer<DispatchResumeWorkflows>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowRequestConsumer"/> class.
    /// </summary>
    public DispatchWorkflowRequestConsumer(IWorkflowRuntime workflowRuntime, IWorkflowInstanceManager workflowInstanceManager)
    {
        _workflowRuntime = workflowRuntime;
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

        var options = new ResumeWorkflowRuntimeParams
        {
            CorrelationId = message.CorrelationId,
            BookmarkId = message.BookmarkId,
            ActivityHandle = message.ActivityHandle,
            Input = message.Input,
            Properties = message.Properties,
            CancellationToken = cancellationToken
        };

        await _workflowRuntime.ResumeWorkflowAsync(message.InstanceId, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchTriggerWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            ActivityInstanceId = message.ActivityInstanceId,
            Input = message.Input,
            Properties = message.Properties,
            CancellationToken = cancellationToken
        };
        await _workflowRuntime.TriggerWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchResumeWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var options = new TriggerWorkflowsOptions
        {
            CorrelationId = message.CorrelationId,
            WorkflowInstanceId = message.WorkflowInstanceId,
            Input = message.Input,
            Properties = message.Properties,
            CancellationToken = cancellationToken
        };

        await _workflowRuntime.ResumeWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }

    private async Task DispatchNewWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.DefinitionId)) throw new ArgumentException("The definition ID is required when dispatching a workflow definition.");
        if (message.VersionOptions == null) throw new ArgumentException("The version options are required when dispatching a workflow definition.");

        var options = new StartWorkflowRuntimeParams
        {
            ParentWorkflowInstanceId = message.ParentWorkflowInstanceId,
            CorrelationId = message.CorrelationId,
            Input = message.Input,
            Properties = message.Properties,
            VersionOptions = message.VersionOptions.Value,
            TriggerActivityId = message.TriggerActivityId,
            InstanceId = message.InstanceId,
            CancellationToken = cancellationToken
        };

        await _workflowRuntime.TryStartWorkflowAsync(message.DefinitionId, options);
    }

    private async Task DispatchExistingWorkflowInstanceAsync(DispatchWorkflowDefinition message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId)) throw new ArgumentException("The instance ID is required when dispatching an existing workflow instance.");

        var options = new StartWorkflowRuntimeParams
        {
            TriggerActivityId = message.TriggerActivityId,
            IsExistingInstance = true,
            InstanceId = message.InstanceId,
            CancellationToken = cancellationToken
        };

        await _workflowRuntime.StartWorkflowAsync(message.InstanceId, options);
    }
}