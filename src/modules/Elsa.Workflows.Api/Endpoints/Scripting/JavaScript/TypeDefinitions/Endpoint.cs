using System.Text;
using Elsa.Common.Models;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;
using Elsa.Workflows.Runtime.Services;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Scripting.JavaScript.TypeDefinitions;

/// <summary>
/// Returns a TypeScript definition that is used by the Monaco editor to display intellisense for JavaScript expressions.
/// </summary>
[PublicAPI]
public class Get : Endpoint<Request>
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ITypeDefinitionService _typeDefinitionService;

    /// <inheritdoc />
    public Get(IWorkflowDefinitionService workflowDefinitionService, ITypeDefinitionService typeDefinitionService)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _typeDefinitionService = typeDefinitionService;
    }
    
    /// <inheritdoc />
    public override void Configure()
    {
        Post("scripting/javascript/type-definitions/{workflowDefinitionId}");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionService.FindAsync(request.WorkflowDefinitionId, VersionOptions.Latest, cancellationToken);

        if (workflowDefinition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var intellisenseContext = new TypeDefinitionContext(workflow, request.ActivityTypeName, request.PropertyName, cancellationToken);
        var typeDefinitions = await _typeDefinitionService.GenerateTypeDefinitionsAsync(intellisenseContext);
        var fileName = $"elsa.{request.WorkflowDefinitionId}.d.ts";
        var data = Encoding.UTF8.GetBytes(typeDefinitions);

        await SendBytesAsync(data, fileName, "application/x-typescript", cancellation: cancellationToken);
    }
}

public record Request(string WorkflowDefinitionId, string? ActivityTypeName, string? PropertyName)
{
    public Request() : this(default!, default, default)
    {
    }
}