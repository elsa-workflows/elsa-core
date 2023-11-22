using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Management.Responses;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Http.Handlers;

/// <summary>
/// A <see cref="ValidateWorkflowRequest"/> handler that validates a workflow path and return any errors. 
/// </summary>
public class ValidateWorkflowRequestHandler : IRequestHandler<ValidateWorkflowRequest, ValidateWorkflowResponse>
{
    private readonly ITriggerStore _triggerStore;
    private readonly ITriggerIndexer _triggerIndexer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ValidateWorkflowRequestHandler(ITriggerStore triggerStore, ITriggerIndexer triggerIndexer)
    {
        _triggerStore = triggerStore;
        _triggerIndexer = triggerIndexer;
    }

    /// <inheritdoc />
    public async Task<ValidateWorkflowResponse> HandleAsync(ValidateWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = request.Workflow;
        var httpEndpointTriggers = (await _triggerIndexer.GetTriggersAsync(workflow, cancellationToken)).Where(x => x.Payload is HttpEndpointBookmarkPayload).ToList();
        var publishedWorkflowsTriggers = (await _triggerStore.FindManyAsync(new TriggerFilter { Name = ActivityTypeNameHelper.GenerateTypeName(typeof(HttpEndpoint)) }, cancellationToken)).ToList();
        var validationErrors = new List<WorkflowValidationError>();

        foreach (var httpEndpointTrigger in httpEndpointTriggers)
        {
            var triggerPayload = httpEndpointTrigger.GetPayload<HttpEndpointBookmarkPayload>();

            var otherWorkflowsWithSamePath = publishedWorkflowsTriggers
                .Where(x =>
                    x.WorkflowDefinitionId != workflow.Identity.DefinitionId &&
                    x.Payload is HttpEndpointBookmarkPayload payload &&
                    payload.Path == triggerPayload.Path &&
                    payload.Method == triggerPayload.Method)
                .ToList();

            if (!otherWorkflowsWithSamePath.Any())
                continue;

            var message = $"The {triggerPayload.Path} path and {triggerPayload.Method} method are already in use by another workflow!";
            validationErrors.Add(new WorkflowValidationError(message, httpEndpointTrigger.ActivityId));
        }

        return new ValidateWorkflowResponse(validationErrors);
    }
}