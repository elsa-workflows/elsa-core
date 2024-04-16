using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class StimulusSender(
    IStimulusHasher stimulusHasher,
    ITriggerBoundWorkflowService triggerBoundWorkflowService,
    IBookmarkBoundWorkflowService bookmarkBoundWorkflowService,
    IWorkflowRuntime workflowRuntime) : IStimulusSender
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
        return new SendStimulusResult(responses);
    }

    private async Task<ICollection<RunWorkflowInstanceResponse>> TriggerNewWorkflowsAsync(string stimulusHash, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var triggerBoundWorkflows = await triggerBoundWorkflowService.FindManyAsync(stimulusHash, cancellationToken).ToList();
        var correlationId = metadata?.CorrelationId;
        var input = metadata?.Input;
        var properties = metadata?.Properties;
        var parentId = metadata?.ParentWorkflowInstanceId;
        var triggerActivityId = metadata?.TriggerActivityId;
        var responses = new List<RunWorkflowInstanceResponse>();

        foreach (TriggerBoundWorkflow triggerBoundWorkflow in triggerBoundWorkflows)
        {
            var workflowClient = await workflowRuntime.CreateClientAsync(cancellationToken);
            var createWorkflowInstanceRequest = new CreateWorkflowInstanceRequest
            {
                CorrelationId = correlationId,
                Input = input,
                Properties = properties,
                ParentId = parentId,
                DefinitionVersionId = triggerBoundWorkflow.Workflow.Identity.Id,
            };
            await workflowClient.CreateInstanceAsync(createWorkflowInstanceRequest, cancellationToken);

            var request = new RunWorkflowInstanceRequest
            {
                Input = input,
                Properties = properties,
                TriggerActivityId = triggerActivityId,
            };
            var response = await workflowClient.RunAsync(request, cancellationToken);
            responses.Add(response);
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
        var bookmarkId = metadata?.BookmarkId;
        var responses = new List<RunWorkflowInstanceResponse>();

        foreach (BookmarkBoundWorkflow bookmarkBoundWorkflow in bookmarkBoundWorkflows)
        {
            var workflowInstanceId = bookmarkBoundWorkflow.WorkflowInstanceId;
            var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);
            var request = new RunWorkflowInstanceRequest
            {
                Input = input,
                Properties = properties,
                ActivityHandle = activityHandle,
                BookmarkId = bookmarkId,
            };
            var response = await workflowClient.RunAsync(request, cancellationToken);
            responses.Add(response);
        }

        return responses;
    }
}