using Elsa.Extensions;
using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Implements <see cref="IWorkflowDispatcher"/> by leveraging MassTransit.
/// </summary>
public class MassTransitWorkflowDispatcher(
    IBus bus,
    IEndpointChannelFormatter endpointChannelFormatter,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceManager workflowInstanceManager,
    IBookmarkHasher bookmarkHasher,
    ITriggerStore triggerStore,
    IBookmarkStore bookmarkStore,
    ILogger<MassTransitWorkflowDispatcher> logger)
    : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflow = await workflowDefinitionService.FindWorkflowAsync(request.DefinitionId, request.VersionOptions, cancellationToken);

        if (workflow == null)
            throw new Exception($"Workflow definition with definition ID '{request.DefinitionId} and version {request.VersionOptions}' not found");

        var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
        {
            Workflow = workflow,
            WorkflowInstanceId = request.InstanceId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId
        };
        
        await DispatchWorkflowAsync(createWorkflowInstanceRequest, request.TriggerActivityId, options, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);

        await sendEndpoint.Send(new DispatchWorkflowInstance(request.InstanceId)
        {
            BookmarkId = request.BookmarkId,
            ActivityId = request.ActivityId,
            ActivityNodeId = request.ActivityNodeId,
            ActivityInstanceId = request.ActivityInstanceId,
            ActivityHash = request.ActivityHash,
            CorrelationId = request.CorrelationId
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        await DispatchTriggersAsync(request, options, cancellationToken);
        await DispatchBookmarksAsync(request, options, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var hash = bookmarkHasher.Hash(request.ActivityTypeName, request.BookmarkPayload, request.ActivityInstanceId);
        var correlationId = request.CorrelationId;
        var workflowInstanceId = request.WorkflowInstanceId;
        var activityInstanceId = request.ActivityInstanceId;
        var filter = new BookmarkFilter { Hash = hash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId, ActivityInstanceId = activityInstanceId };
        var bookmarks = await bookmarkStore.FindManyAsync(filter, cancellationToken);
        await DispatchBookmarksAsync(bookmarks, request.Input, null, options, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    private async Task DispatchTriggersAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var triggerHash = bookmarkHasher.Hash(request.ActivityTypeName, request.BookmarkPayload);
        var triggerFilter = new TriggerFilter { Hash = triggerHash };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();

        foreach (var trigger in triggers)
        {
            var workflow = await workflowDefinitionService.FindWorkflowAsync(trigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflow == null)
            {
                logger.LogWarning("Workflow definition with ID '{WorkflowDefinitionId}' not found", trigger.WorkflowDefinitionVersionId);
                continue;
            }

            var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
            {
                Workflow = workflow,
                WorkflowInstanceId = request.WorkflowInstanceId,
                Input = request.Input,
                Properties = request.Properties,
                CorrelationId = request.CorrelationId
            };

            await DispatchWorkflowAsync(createWorkflowInstanceRequest, trigger.ActivityId, options, cancellationToken);
        }
    }

    private async Task DispatchWorkflowAsync(CreateWorkflowInstanceRequest createWorkflowInstanceRequest, string? triggerActivityId, DispatchWorkflowOptions? options, CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceManager.CreateWorkflowInstanceAsync(createWorkflowInstanceRequest, cancellationToken);
        var sendEndpoint = await GetSendEndpointAsync(options);
        var message = DispatchWorkflowDefinition.DispatchExistingWorkflowInstance(workflowInstance.Id, triggerActivityId);
        await sendEndpoint.Send(message, cancellationToken);
    }

    private async Task DispatchBookmarksAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId;
        var workflowInstanceId = request.WorkflowInstanceId;
        var activityInstanceId = request.ActivityInstanceId;
        var bookmarkHash = bookmarkHasher.Hash(request.ActivityTypeName, request.BookmarkPayload, activityInstanceId);

        var filter = new BookmarkFilter { Hash = bookmarkHash, CorrelationId = correlationId, WorkflowInstanceId = workflowInstanceId, ActivityInstanceId = activityInstanceId };
        var bookmarks = (await bookmarkStore.FindManyAsync(filter, cancellationToken)).ToList();

        await DispatchBookmarksAsync(bookmarks, request.Input, request.Properties, options, cancellationToken);
    }

    private async Task DispatchBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, IDictionary<string, object>? input, IDictionary<string, object>? properties, DispatchWorkflowOptions? options, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            if (input != null || properties != null)
            {
                var workflowInstance = await workflowInstanceManager.FindByIdAsync(bookmark.WorkflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    logger.LogWarning("Workflow instance with ID '{WorkflowInstanceId}' not found", bookmark.WorkflowInstanceId);
                    continue;
                }

                if (input != null) workflowInstance.WorkflowState.Input.Merge(input);
                if (properties != null) workflowInstance.WorkflowState.Properties.Merge(properties);

                await workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
            }

            var dispatchInstanceRequest = new DispatchWorkflowInstanceRequest(bookmark.BookmarkId) { BookmarkId = bookmark.BookmarkId, CorrelationId = bookmark.CorrelationId };
            await DispatchAsync(dispatchInstanceRequest, options, cancellationToken);
        }
    }

    private async Task<ISendEndpoint> GetSendEndpointAsync(DispatchWorkflowOptions? options = default)
    {
        var endpointName = endpointChannelFormatter.FormatEndpointName(options?.Channel);
        var sendEndpoint = await bus.GetSendEndpoint(new Uri($"queue:{endpointName}"));
        return sendEndpoint;
    }
}