using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Variables.Post;

[UsedImplicitly]
internal class List(
    IWorkflowInstanceManager workflowInstanceManager,
    //IWorkflowInstanceStore workflowInstanceStore, 
    IWorkflowDefinitionService workflowDefinitionService, 
    IServiceProvider serviceProvider, 
    IWorkflowInstanceVariableWriter variableWriter) : ElsaEndpoint<Request, ListResponse<ResolvedVariableModel>>
{
    public override void Configure()
    {
        Post("/workflow-instances/{id}/variables");
        ConfigurePermissions("write:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowInstanceId = Route<string>("id");
        
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            AddError("The workflow instance ID is required.");
            await SendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }
        
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);
        
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
        
        var resolvedVariables = await variableWriter.SetVariables(workflowExecutionContext, request.Variables, cancellationToken).ToList();
        await workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
        var variableModels = resolvedVariables.Select(x => new ResolvedVariableModel(x.Variable.Id, x.Variable.Name, x.Value)).ToList();
        var response = new ListResponse<ResolvedVariableModel>(variableModels);
        await SendOkAsync(response, cancellationToken);
    }
}

internal class Request
{
    public ICollection<VariableUpdateValue> Variables { get; set; }
}

internal record ResolvedVariableModel(string Id, string Name, object? Value);