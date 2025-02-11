using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Materializers;

/// <summary>
/// A workflow materializer that deserializes workflows created in C# code.
/// </summary>
public class ClrWorkflowMaterializer(
    IPayloadSerializer payloadSerializer,
    IWorkflowBuilderFactory workflowBuilderFactory,
    IServiceProvider serviceProvider) : IWorkflowMaterializer
{
    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "CLR";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <inheritdoc />
    public async ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var providerContext = payloadSerializer.Deserialize<ClrWorkflowMaterializerContext>(definition.MaterializerContext!);
        var workflowBuilderType = providerContext.WorkflowBuilderType == null! ? typeof(NotFoundWorkflowBuilder) : providerContext.WorkflowBuilderType;
        var workflowBuilder = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, workflowBuilderType);
        var workflowDefinitionBuilder = workflowBuilderFactory.CreateBuilder();
        var workflow = await workflowDefinitionBuilder.BuildWorkflowAsync(workflowBuilder, cancellationToken);

        // Assign identities from the definition.
        workflow.Identity = new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id, definition.TenantId);

        return workflow;
    }
}

/// <summary>
/// Provides context for the CLR workflow materializer.
/// </summary>
/// <param name="WorkflowBuilderType">The type of the workflow builder.</param>
public record ClrWorkflowMaterializerContext(Type WorkflowBuilderType);

/// <summary>
/// A workflow builder that is used when the workflow builder type is not found.
/// </summary>
public class NotFoundWorkflowBuilder : WorkflowBase
{
}