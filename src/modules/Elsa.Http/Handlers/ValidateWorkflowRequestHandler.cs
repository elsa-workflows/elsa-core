using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Http.Handlers;

/// <summary>
/// A <see cref="ValidateWorkflowRequest"/> handler that validates a workflow path and return any errors. 
/// </summary>
public class ValidateWorkflowRequestHandler : IRequestHandler<ValidateWorkflowRequest, ValidateWorkflowResponse>
{
    private readonly ITriggerStore _triggerStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IActivityVisitor _activityVisitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger _logger;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public ValidateWorkflowRequestHandler(
        ITriggerStore triggerStore,
        IWorkflowDefinitionService workflowDefinitionService,
        IActivityVisitor activityVisitor,
        IServiceProvider serviceProvider,
        IExpressionEvaluator expressionEvaluator,
        ILogger<ValidateWorkflowRequestHandler> logger)
    {
        _triggerStore = triggerStore;
        _workflowDefinitionService = workflowDefinitionService;
        _activityVisitor = activityVisitor;
        _serviceProvider = serviceProvider;
        _expressionEvaluator = expressionEvaluator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidateWorkflowResponse> HandleAsync(ValidateWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflowDefinition = request.WorkflowDefinition;
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowIndexingContext = new WorkflowIndexingContext(workflow, cancellationToken);
        var nodes = (await _activityVisitor.VisitAsync(workflow.Root, cancellationToken)).Flatten().ToList();
        
        var pathsToValidate = new List<string>();
        foreach (var node in nodes.Where(n => n.Activity is HttpEndpoint))
        {
            if (node.Activity is not ITrigger trigger) continue;

            var expressionExecutionContext = await trigger.CreateExpressionExecutionContextAsync(_serviceProvider, workflowIndexingContext, _expressionEvaluator, _logger);
            var triggerIndexingContext = new TriggerIndexingContext(workflowIndexingContext, expressionExecutionContext, trigger, cancellationToken);
            var payloads = (await trigger.GetTriggerPayloadsAsync(triggerIndexingContext)).ToList();

            pathsToValidate.AddRange(payloads.OfType<HttpEndpointBookmarkPayload>().Select(p => p.Path));
        }
        
        var publishedWorkflowsTriggers = (await _triggerStore.FindManyAsync(new TriggerFilter { Name = ActivityTypeNameHelper.GenerateTypeName(typeof(HttpEndpoint)) }, cancellationToken)).ToList();
        
        var invalidPaths = pathsToValidate.Where(path => publishedWorkflowsTriggers.Any(t => t.WorkflowDefinitionId != workflowDefinition.DefinitionId && t.Payload is HttpEndpointBookmarkPayload && t.GetPayload<HttpEndpointBookmarkPayload>().Path == path)).ToList();

        return invalidPaths.Any() ? 
            new ValidateWorkflowResponse(new[] { $"The following paths are already in use by other workflows: {string.Join(", ", invalidPaths)}" }) :
            new ValidateWorkflowResponse(Enumerable.Empty<string>());
    }
}