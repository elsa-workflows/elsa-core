using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Materializers;

/// <summary>
/// A workflow materializer that deserializes workflows created in C# code.
/// </summary>
public class ClrWorkflowMaterializer : IWorkflowMaterializer
{
    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "CLR";

    private readonly IPayloadSerializer _payloadSerializer;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClrWorkflowMaterializer"/> class.
    /// </summary>
    public ClrWorkflowMaterializer(
        IPayloadSerializer payloadSerializer,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IServiceProvider serviceProvider
    )
    {
        _payloadSerializer = payloadSerializer;
        _workflowBuilderFactory = workflowBuilderFactory;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <inheritdoc />
    public async ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var providerContext = _payloadSerializer.Deserialize<ClrWorkflowMaterializerContext>(definition.MaterializerContext!);
        var workflowBuilderType = providerContext.WorkflowBuilderType;
        var workflowBuilder = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, workflowBuilderType);
        var workflowDefinitionBuilder = _workflowBuilderFactory.CreateBuilder();
        var workflow = await workflowDefinitionBuilder.BuildWorkflowAsync(workflowBuilder, cancellationToken);

        // Assign identities from the definition.
        workflow.Identity = new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id);

        return workflow;
    }
}

/// <summary>
/// Provides context for the CLR workflow materializer.
/// </summary>
/// <param name="WorkflowBuilderType">The type of the workflow builder.</param>
public record ClrWorkflowMaterializerContext(Type WorkflowBuilderType);