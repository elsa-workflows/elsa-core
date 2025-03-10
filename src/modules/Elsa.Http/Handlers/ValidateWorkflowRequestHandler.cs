using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Http.Handlers;

/// <summary>
/// A <see cref="ValidateWorkflowRequest"/> handler that validates a workflow path and return any errors.
/// </summary>
[UsedImplicitly]
public class ValidateWorkflowRequestHandler : INotificationHandler<WorkflowDefinitionValidating>
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
    
    public async Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var workflow = notification.Workflow;
        var httpEndpointTriggers = (await _triggerIndexer.GetTriggersAsync(workflow, cancellationToken)).Where(x => x.Payload is HttpEndpointBookmarkPayload).ToList();
        var filter = new TriggerFilter
        {
            Name = ActivityTypeNameHelper.GenerateTypeName(typeof(HttpEndpoint))
        };
        var publishedWorkflowsTriggers = (await _triggerStore.FindManyAsync(filter, cancellationToken)).ToList();
        var validationErrors = notification.ValidationErrors;

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
            validationErrors.Add(new(message, httpEndpointTrigger.ActivityId));
        }
    }
}
