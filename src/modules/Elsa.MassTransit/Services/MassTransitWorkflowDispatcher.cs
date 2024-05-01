using Elsa.Extensions;
using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
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
    IStimulusHasher stimulusHasher,
    ITriggerStore triggerStore,
    IBookmarkStore bookmarkStore,
    ILogger<MassTransitWorkflowDispatcher> logger)
    : IWorkflowDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(request.DefinitionVersionId, cancellationToken);

        if (workflowGraph == null)
            throw new Exception($"Workflow definition version with ID '{request.DefinitionVersionId}' not found");

        var workflowInstanceOptions = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = request.InstanceId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId
        };

        await DispatchWorkflowAsync(workflowGraph, workflowInstanceOptions, request.TriggerActivityId, options, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);

        await sendEndpoint.Send(new DispatchWorkflowInstance(request.InstanceId)
        {
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
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
        var hash = stimulusHasher.Hash(request.ActivityTypeName, request.BookmarkPayload, request.ActivityInstanceId);
        var correlationId = request.CorrelationId;
        var workflowInstanceId = request.WorkflowInstanceId;
        var activityInstanceId = request.ActivityInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = hash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId
        };
        var bookmarks = await bookmarkStore.FindManyAsync(filter, cancellationToken);
        await DispatchBookmarksAsync(bookmarks, request.Input, null, options, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    private async Task DispatchTriggersAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var triggerHash = stimulusHasher.Hash(request.ActivityTypeName, request.BookmarkPayload);
        var triggerFilter = new TriggerFilter
        {
            Hash = triggerHash
        };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();

        foreach (var trigger in triggers)
        {
            var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(trigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflowGraph == null)
            {
                logger.LogWarning("Workflow definition with ID '{WorkflowDefinitionId}' not found", trigger.WorkflowDefinitionVersionId);
                continue;
            }

            var workflowInstanceOptions = new WorkflowInstanceOptions()
            {
                WorkflowInstanceId = request.WorkflowInstanceId,
                Input = request.Input,
                Properties = request.Properties,
                CorrelationId = request.CorrelationId
            };

            await DispatchWorkflowAsync(workflow, workflowInstanceOptions, trigger.ActivityId, options, cancellationToken);
        }
    }

    private async Task DispatchWorkflowAsync(Workflow workflow, WorkflowInstanceOptions? workflowInstanceOptions, string? triggerActivityId, DispatchWorkflowOptions? options, CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceManager.CreateWorkflowInstanceAsync(workflow, workflowInstanceOptions, cancellationToken);
        var sendEndpoint = await GetSendEndpointAsync(options);
        var message = DispatchWorkflowDefinition.DispatchExistingWorkflowInstance(workflowInstance.Id, triggerActivityId);
        await sendEndpoint.Send(message, cancellationToken);
    }

    private async Task DispatchBookmarksAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId;
        var workflowInstanceId = request.WorkflowInstanceId;
        var activityInstanceId = request.ActivityInstanceId;
        var bookmarkHash = stimulusHasher.Hash(request.ActivityTypeName, request.BookmarkPayload, activityInstanceId);

        var filter = new BookmarkFilter
        {
            Hash = bookmarkHash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId
        };
        var bookmarks = (await bookmarkStore.FindManyAsync(filter, cancellationToken)).ToList();

        await DispatchBookmarksAsync(bookmarks, request.Input, request.Properties, options, cancellationToken);
    }

    private async Task DispatchBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, IDictionary<string, object>? input, IDictionary<string, object>? properties, DispatchWorkflowOptions? options, CancellationToken cancellationToken)
    {
        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;

            if (input != null || properties != null)
            {
                var workflowInstance = await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    logger.LogWarning("Workflow instance with ID '{WorkflowInstanceId}' not found", workflowInstanceId);
                    continue;
                }

                if (input != null) workflowInstance.WorkflowState.Input.Merge(input);
                if (properties != null) workflowInstance.WorkflowState.Properties.Merge(properties);

                await workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
            }

            var dispatchInstanceRequest = new DispatchWorkflowInstanceRequest(workflowInstanceId)
            {
                BookmarkId = bookmark.BookmarkId,
                CorrelationId = bookmark.CorrelationId
            };
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