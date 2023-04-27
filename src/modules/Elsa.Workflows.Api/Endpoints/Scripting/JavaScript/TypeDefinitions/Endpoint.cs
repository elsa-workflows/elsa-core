using System.Text;
using Elsa.Common.Models;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Endpoints.Scripting.JavaScript.TypeDefinitions;

/// <summary>
/// Returns a TypeScript definition that is used by the Monaco editor to display intellisense for JavaScript expressions.
/// </summary>
[PublicAPI]
internal class Get : Endpoint<Request>
{
    private readonly ITypeDefinitionService _typeDefinitionService;
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public Get(ITypeDefinitionService typeDefinitionService, IServiceProvider serviceProvider)
    {
        _typeDefinitionService = typeDefinitionService;
        _serviceProvider = serviceProvider;
    }
    
    /// <inheritdoc />
    public override void Configure()
    {
        Post("scripting/javascript/type-definitions/{workflowDefinitionId}");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowDefinition = await GetWorkflowDefinition(request.WorkflowDefinitionId, cancellationToken);

        if (workflowDefinition == null)
        {
            AddError($"Workflow definition {request.WorkflowDefinitionId} not found");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        var typeDefinitionContext = new TypeDefinitionContext(workflowDefinition, request.ActivityTypeName, request.PropertyName, cancellationToken);
        var typeDefinitions = await _typeDefinitionService.GenerateTypeDefinitionsAsync(typeDefinitionContext);
        var fileName = $"elsa.{request.WorkflowDefinitionId}.d.ts";
        var data = Encoding.UTF8.GetBytes(typeDefinitions);

        await SendBytesAsync(data, fileName, "application/x-typescript", cancellation: cancellationToken);
    }

    private async Task<WorkflowDefinition?> GetWorkflowDefinition(string workflowDefinitionId, CancellationToken cancellationToken)
    {
        var workflowDefinitionService = _serviceProvider.GetService<IWorkflowDefinitionService>();
        var workflowDefinition = workflowDefinitionService != null ? await workflowDefinitionService.FindAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken) : default;
        return workflowDefinition;
    }
}

internal record Request(string WorkflowDefinitionId, string? ActivityTypeName, string? PropertyName)
{
    public Request() : this(default!, default!, default)
    {
    }
}