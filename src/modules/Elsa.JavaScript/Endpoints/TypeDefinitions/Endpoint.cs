using System.Text;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.JavaScript.Endpoints.TypeDefinitions;

/// <summary>
/// Returns a TypeScript definition that is used by the Monaco editor to display intellisense for JavaScript expressions.
/// </summary>
[PublicAPI]
internal class Get : ElsaEndpoint<Request>
{
    private readonly ITypeDefinitionService _typeDefinitionService;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    /// <inheritdoc />
    public Get(ITypeDefinitionService typeDefinitionService, IWorkflowDefinitionService workflowDefinitionService)
    {
        _typeDefinitionService = typeDefinitionService;
        _workflowDefinitionService = workflowDefinitionService;
    }
    
    /// <inheritdoc />
    public override void Configure()
    {
        Post("scripting/javascript/type-definitions/{workflowDefinitionId}");
        ConfigurePermissions("read:*", "read:javascript-type-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflow = await GetWorkflowAsync(request.WorkflowDefinitionId, cancellationToken);

        if (workflow == null)
        {
            AddError($"Workflow definition {request.WorkflowDefinitionId} not found");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        var typeDefinitionContext = new TypeDefinitionContext(workflow, request.ActivityTypeName, request.PropertyName, cancellationToken);
        var typeDefinitions = await _typeDefinitionService.GenerateTypeDefinitionsAsync(typeDefinitionContext);
        var fileName = $"elsa.{request.WorkflowDefinitionId}.d.ts";
        var data = Encoding.UTF8.GetBytes(typeDefinitions);

        await SendBytesAsync(data, fileName, "application/x-typescript", cancellation: cancellationToken);
    }

    private async Task<Workflow?> GetWorkflowAsync(string workflowDefinitionId, CancellationToken cancellationToken)
    {
        return await _workflowDefinitionService.FindWorkflowAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken);
    }
}

internal record Request(string WorkflowDefinitionId, string? ActivityTypeName, string? PropertyName)
{
    public Request() : this(default!, default!, default)
    {
    }
}