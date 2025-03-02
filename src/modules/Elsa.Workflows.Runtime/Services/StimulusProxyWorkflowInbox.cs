using Elsa.Common;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a proxy for sending stimulus to the workflow runtime while <see cref="IWorkflowInbox"/> is being deprecated.
/// </summary>
[Obsolete("Please use the Stimulus API instead")]
public class StimulusProxyWorkflowInbox(
    IStimulusSender stimulusSender,
    IWorkflowDispatcher workflowDispatcher,
    ISystemClock systemClock,
    IIdentityGenerator identityGenerator,
    IHasher bookmarkHasher,
    ILogger<StimulusProxyWorkflowInbox> logger)
    : IWorkflowInbox
{
    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var defaultOptions = new WorkflowInboxMessageDeliveryParams();
        return await SubmitAsync(message, defaultOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage newMessage, WorkflowInboxMessageDeliveryParams @params, CancellationToken cancellationToken = default)
    {
        var activityTypeName = newMessage.ActivityTypeName;
        var stimulus = newMessage.BookmarkPayload;
        var stimulusMetadata = new StimulusMetadata
        {
            CorrelationId = newMessage.CorrelationId,
            WorkflowInstanceId = newMessage.WorkflowInstanceId,
            ActivityInstanceId = newMessage.ActivityInstanceId,
            Input = newMessage.Input
        };

        var now = systemClock.UtcNow;

        var message = new WorkflowInboxMessage
        {
            Id = identityGenerator.GenerateId(),
            CreatedAt = now,
            ExpiresAt = now + newMessage.TimeToLive,
            ActivityInstanceId = newMessage.ActivityInstanceId,
            CorrelationId = newMessage.CorrelationId,
            WorkflowInstanceId = newMessage.WorkflowInstanceId,
            ActivityTypeName = newMessage.ActivityTypeName,
            BookmarkPayload = newMessage.BookmarkPayload,
            Input = newMessage.Input,
            Hash = bookmarkHasher.Hash(newMessage.ActivityTypeName, newMessage.BookmarkPayload, newMessage.ActivityInstanceId),
        };

        var result = await stimulusSender.SendAsync(activityTypeName, stimulus, stimulusMetadata, cancellationToken);
        var workflowExecutionResults = Map(result.WorkflowInstanceResponses).ToList();

        return new SubmitWorkflowInboxMessageResult(message, workflowExecutionResults);
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> DeliverAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        await ResumeWorkflowsAsynchronouslyAsync(message, cancellationToken);
        return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> BroadcastAsync(WorkflowInboxMessage message, BroadcastWorkflowInboxMessageOptions? options, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        if (workflowInstanceId != null)
        {
            if (options?.DispatchAsynchronously == true)
            {
                await ResumeWorkflowsAsynchronouslyAsync(message, cancellationToken);
                return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
            }

            var results = await ResumeWorkflowsSynchronouslyAsync(message, cancellationToken);
            return new DeliverWorkflowInboxMessageResult(results.ToList());
        }

        if (options?.DispatchAsynchronously == false)
        {
            var result = await stimulusSender.SendAsync(activityTypeName, bookmarkPayload, new StimulusMetadata
            {
                CorrelationId = correlationId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityInstanceId = activityInstanceId,
                Input = input
            }, cancellationToken);

            var workflowExecutionResults = Map(result.WorkflowInstanceResponses).ToList();
            return new DeliverWorkflowInboxMessageResult(workflowExecutionResults);
        }

        var dispatchRequest = new DispatchTriggerWorkflowsRequest(activityTypeName, bookmarkPayload)
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        };
        await workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);
        return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
    }

    private async Task ResumeWorkflowsAsynchronouslyAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        await workflowDispatcher.DispatchAsync(new DispatchResumeWorkflowsRequest(activityTypeName, bookmarkPayload)
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        }, cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<WorkflowExecutionResult>> ResumeWorkflowsSynchronouslyAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        var result = await stimulusSender.SendAsync(activityTypeName, bookmarkPayload, new StimulusMetadata
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        }, cancellationToken);

        return Map(result.WorkflowInstanceResponses).ToList();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("The workflow inbox API is deprecated and will be removed in a future version. Please use the stimulus API instead. This method will return an empty list");
        return new([]);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("The workflow inbox API is deprecated and will be removed in a future version. Please use the stimulus API instead. This method will return an empty list");
        return new([]);
    }

    private static IEnumerable<WorkflowExecutionResult> Map(IEnumerable<RunWorkflowInstanceResponse> source)
    {
        return source.Select(response => new WorkflowExecutionResult(
            response.WorkflowInstanceId,
            response.Status,
            response.SubStatus,
            new List<Bookmark>(),
            response.Incidents)
        );
    }
}