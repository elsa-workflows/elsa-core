using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Results;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class StimulusSender(
    IStimulusHasher stimulusHasher,
    ITriggerBoundWorkflowService triggerBoundWorkflowService,
    IWorkflowResumer workflowResumer,
    IBookmarkQueue bookmarkQueue,
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
        var input = metadata?.Input;
        var properties = metadata?.Properties;
        
        var bookmarkFilter = new BookmarkFilter
        {
            Hash = stimulusHash,
            CorrelationId = metadata?.CorrelationId,
            WorkflowInstanceId = metadata?.WorkflowInstanceId,
            ActivityInstanceId = metadata?.ActivityInstanceId,
            BookmarkId = metadata?.BookmarkId
        };
        var responses = (await workflowResumer.ResumeAsync(bookmarkFilter, new()
        {
            Input = input,
            Properties = properties
        }, cancellationToken)).ToList();

        if (responses.Count > 0)
        {
            logger.LogDebug("Successfully resumed {WorkflowCount} workflow instances using stimulus {StimulusHash}", responses.Count, stimulusHash);
            return responses;
        }

        // If no bookmarks were matched, enqueue the request in case a matching bookmark is created in the near future.
        var workflowInstanceId = metadata?.WorkflowInstanceId;

        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = workflowInstanceId,
            BookmarkId = metadata?.BookmarkId,
            CorrelationId = metadata?.CorrelationId,
            StimulusHash = stimulusHash,
            Options = new()
            {
                Input = input,
                Properties = properties
            }
        };
            
        logger.LogDebug("Bookmark queue item enqueued with stimulus: {StimulusHash}", bookmarkQueueItem.StimulusHash);
            
        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);

        return responses;
    }
}