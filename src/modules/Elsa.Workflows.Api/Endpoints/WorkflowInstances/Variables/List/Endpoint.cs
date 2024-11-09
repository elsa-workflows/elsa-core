using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Variables.List;

[UsedImplicitly]
internal class List(IWorkflowInstanceVariableManager workflowInstanceVariableManager) : ElsaEndpointWithoutRequest<ListResponse<ResolvedVariableModel>>
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
        
        var variables = await workflowInstanceVariableManager.GetVariablesAsync(workflowInstanceId, cancellationToken).ToList();
        var variableModels = variables.Select(x => new ResolvedVariableModel(x.Variable.Id, x.Variable.Name, x.Value)).ToList();
        var response = new ListResponse<ResolvedVariableModel>(variableModels);
        await SendOkAsync(response, cancellationToken);
    }
}

internal record ResolvedVariableModel(string Id, string Name, object? Value);