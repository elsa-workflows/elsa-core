using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class StimulusSender(
    IStimulusHasher stimulusHasher,
    ITriggerBoundWorkflowService triggerBoundWorkflowService,
    IBookmarkBoundWorkflowService bookmarkBoundWorkflowService,
    IBookmarkQueue bookmarkQueue,
    IWorkflowRuntime workflowRuntime,
    ITriggerInvoker triggerInvoker,
    ILogger<StimulusSender> logger) : IStimulusSender
{
    /// <inheritdoc />
    public Task<SendStimulusResult> SendAsync(string activityTypeName, object stimulus, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var stimulusHash = stimulusHasher.Hash(activityTypeName, stimulus, metadata?.ActivityInstanceId);
        return SendAsync(stimulusHash, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SendStimulusResult> SendAsync(string stimulusHash, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var responses = new List<RunWorkflowInstanceResponse>();

        if (metadata == null || (metadata.WorkflowInstanceId == null && metadata.BookmarkId == null && metadata.ActivityInstanceId == null))
        {
            var triggered = await TriggerNewWorkflowsAsync(stimulusHash, metadata, cancellationToken);
            responses.AddRange(triggered);
        }

        var resumed = await ResumeExistingWorkflowsAsync(stimulusHash, metadata, cancellationToken);
        responses.AddRange(resumed);
        return new(responses);
    }

    private async Task<ICollection<RunWorkflowInstanceResponse>> TriggerNewWorkflowsAsync(string stimulusHash, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var triggerBoundWorkflows = await triggerBoundWorkflowService.FindManyAsync(stimulusHash, cancellationToken).ToList();
        var correlationId = metadata?.CorrelationId;
        var input = metadata?.Input;
        var properties = metadata?.Properties;
        var parentId = metadata?.ParentWorkflowInstanceId;
        var responses = new List<RunWorkflowInstanceResponse>();

        foreach (var triggerBoundWorkflow in triggerBoundWorkflows)
        {
            var workflowGraph = triggerBoundWorkflow.WorkflowGraph;
            var workflow = workflowGraph.Workflow;

            foreach (var trigger in triggerBoundWorkflow.Triggers)
            {
                var triggerRequest = new InvokeTriggerRequest
                {
                    CorrelationId = correlationId,
                    Workflow = workflow,
                    ActivityId = trigger.ActivityId,
                    Input = input,
                    Properties = properties,
                    ParentWorkflowInstanceId = parentId
                };
                
                var response = await triggerInvoker.InvokeAsync(triggerRequest, cancellationToken);
                
                if (response.CannotStart)
                {
                    logger.LogWarning("Workflow activation strategy disallowed starting workflow {WorkflowDefinitionHandle} with correlation ID {CorrelationId}", workflow.DefinitionHandle, correlationId);
                    continue;
                }
                
                responses.Add(response.ToRunWorkflowInstanceResponse());
            }
        }

        return responses;
    }

    private async Task<ICollection<RunWorkflowInstanceResponse>> ResumeExistingWorkflowsAsync(string stimulusHash, StimulusMetadata? metadata, CancellationToken cancellationToken)
    {
        var bookmarkOptions = metadata != null
            ? new FindBookmarkOptions
            {
                CorrelationId = metadata.CorrelationId,
                WorkflowInstanceId = metadata.WorkflowInstanceId,
                ActivityInstanceId = metadata.ActivityInstanceId,
            }
            : null;
        var bookmarkBoundWorkflows = await bookmarkBoundWorkflowService.FindManyAsync(stimulusHash, bookmarkOptions, cancellationToken).ToList();
        var input = metadata?.Input;
        var properties = metadata?.Properties;
        var activityHandle = metadata?.ActivityInstanceId != null ? ActivityHandle.FromActivityInstanceId(metadata.ActivityInstanceId) : null;
        var responses = new List<RunWorkflowInstanceResponse>();

        if (bookmarkBoundWorkflows.Count > 0)
        {
            foreach (var bookmarkBoundWorkflow in bookmarkBoundWorkflows)
            {
                var workflowInstanceId = bookmarkBoundWorkflow.WorkflowInstanceId;
                var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);

                foreach (var storedBookmark in bookmarkBoundWorkflow.Bookmarks)
                {
                    var request = new RunWorkflowInstanceRequest
                    {
                        Input = input,
                        Properties = properties,
                        ActivityHandle = activityHandle,
                        BookmarkId = storedBookmark.Id,
                    };
                    var response = await workflowClient.RunInstanceAsync(request, cancellationToken);
                    responses.Add(response);
                }
            }
        }
        else
        {
            // If no bookmarks were matched, enqueue the request in case a matching bookmark is created in the near future.
            var workflowInstanceId = metadata?.WorkflowInstanceId;

            var bookmarkQueueItem = new NewBookmarkQueueItem
            {
                WorkflowInstanceId = workflowInstanceId,
                BookmarkId = metadata?.BookmarkId,
                StimulusHash = stimulusHash,
                Options = new()
                {
                    Input = input,
                    Properties = properties
                }
            };
            await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
        }

        return responses;
    }
}