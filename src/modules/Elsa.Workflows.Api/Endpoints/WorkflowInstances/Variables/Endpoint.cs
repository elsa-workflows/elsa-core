using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Variables;

[UsedImplicitly]
internal class List(
    IWorkflowInstanceStore workflowInstanceStore, 
    IWorkflowDefinitionService workflowDefinitionService, 
    IServiceProvider serviceProvider, 
    IWorkflowInstanceVariableEnumerator variableEnumerator) : ElsaEndpointWithoutRequest<ListResponse<ResolvedVariableModel>>
{
    public override void Configure()
    {
        Get("/workflow-instances/{id}/variables");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var workflowInstanceId = Route<string>("id");
        
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            AddError("The workflow instance ID is required.");
            await SendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }
        
        var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId, cancellationToken);
        
        if (workflowInstance == null)
        {
            AddError($"Workflow instance with ID {workflowInstanceId} was not found.");
            await SendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
            return;
        }
        
        var workflowState = workflowInstance.WorkflowState;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowState.DefinitionVersionId, cancellationToken);
        
        if (workflowGraph == null)
        {
            AddError($"Workflow definition with ID {workflowState.DefinitionVersionId} was not found.");
            await SendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
            return;
        }
        
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            workflowState,
            cancellationToken: cancellationToken);
        
        var variables = await variableEnumerator.EnumerateVariables(workflowExecutionContext, cancellationToken).ToList();
        var variableModels = variables.Select(x => new ResolvedVariableModel(x.Variable.Id, x.Variable.Name, x.Value)).ToList();
        var response = new ListResponse<ResolvedVariableModel>(variableModels);
        await SendOkAsync(response, cancellationToken);
    }
}

internal record ResolvedVariableModel(string Id, string Name, object? Value);