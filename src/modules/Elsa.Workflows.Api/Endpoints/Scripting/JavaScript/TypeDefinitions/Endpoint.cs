using System.Text;
using Elsa.Common.Models;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Services;
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
        Post("scripting/javascript/type-definitions/{containerId}");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var variables = await GetVariables(request.ContainerType, request.ContainerId, cancellationToken);
        var intellisenseContext = new TypeDefinitionContext(variables, request.ActivityTypeName, request.PropertyName, cancellationToken);
        var typeDefinitions = await _typeDefinitionService.GenerateTypeDefinitionsAsync(intellisenseContext);
        var fileName = $"elsa.{request.ContainerId}.d.ts";
        var data = Encoding.UTF8.GetBytes(typeDefinitions);

        await SendBytesAsync(data, fileName, "application/x-typescript", cancellation: cancellationToken);
    }

    private async Task<ICollection<Variable>> GetVariables(string containerType, string containerId, CancellationToken cancellationToken)
    {
        // TODO: Make this extensible.

        switch (containerType)
        {
            case "workflow-definition":
            {
                var workflowDefinitionService = _serviceProvider.GetService<IWorkflowDefinitionService>();
                var workflowDefinition = workflowDefinitionService != null ? await workflowDefinitionService.FindAsync(containerId, VersionOptions.Latest, cancellationToken) : default;
                return workflowDefinition?.Variables ?? new List<Variable>();
            }
        }

        return new List<Variable>();
    }
}

internal record Request(string ContainerType, string ContainerId, string? ActivityTypeName, string? PropertyName)
{
    public Request() : this(default!, default!, default, default)
    {
    }
}