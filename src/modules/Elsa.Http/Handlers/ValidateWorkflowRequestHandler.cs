using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Management.Responses;
using Elsa.Workflows.Runtime.Contracts;

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
        var pathsToValidate = httpEndpointTriggers.Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        var publishedWorkflowsTriggers = (await _triggerStore.FindManyAsync(new TriggerFilter { Name = ActivityTypeNameHelper.GenerateTypeName(typeof(HttpEndpoint)) }, cancellationToken)).ToList();
        
        var invalidPaths = pathsToValidate
            .Where(path => publishedWorkflowsTriggers
                .Any(t => t.WorkflowDefinitionId != workflow.Identity.DefinitionId && t.Payload is HttpEndpointBookmarkPayload payload && payload.Path == path))
            .ToList();
        
        var validationErrors = new List<string>();
        
        if(invalidPaths.Any())
            validationErrors.Add($"The following paths are already in use by other workflows: {string.Join(", ", invalidPaths)}");

        return new ValidateWorkflowResponse(validationErrors);
    }
}