using Elsa.MassTransit.Messages;
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
    public DispatchWorkflowRequestConsumer(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowDefinition> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var options = new StartWorkflowRuntimeParams
        {
            ParentWorkflowInstanceId = message.ParentWorkflowInstanceId,
            CorrelationId = message.CorrelationId,
            Input = message.Input,
            Properties = message.Properties,
            VersionOptions = message.VersionOptions,
            TriggerActivityId = message.TriggerActivityId,
            InstanceId = message.InstanceId,
            CancellationToken = cancellationToken
        };

        await _workflowRuntime.TryStartWorkflowAsync(message.DefinitionId, options);
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
}