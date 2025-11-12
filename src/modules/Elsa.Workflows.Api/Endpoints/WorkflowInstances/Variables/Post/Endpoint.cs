using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Variables.Post;

[UsedImplicitly]
internal class List(IWorkflowInstanceVariableManager workflowInstanceVariableManager) : ElsaEndpoint<Request, ListResponse<ResolvedVariableModel>>
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
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var resolvedVariables = await workflowInstanceVariableManager.SetVariablesAsync(workflowInstanceId, request.Variables, cancellationToken).ToList();
        var variableModels = resolvedVariables.Select(x => new ResolvedVariableModel(x.Variable.Id, x.Variable.Name, x.Value)).ToList();
        var response = new ListResponse<ResolvedVariableModel>(variableModels);
        await Send.OkAsync(response, cancellationToken);
    }
}

internal class Request
{
    public ICollection<VariableUpdateValue> Variables { get; set; } = null!;
}

internal record ResolvedVariableModel(string Id, string Name, object? Value);