using System.Text.Json;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Materializers;

/// <summary>
/// Materializes a <see cref="Workflow"/> from a <see cref="WorkflowDefinition"/>'s JSON data.
/// </summary>
public class JsonWorkflowMaterializer : IWorkflowMaterializer
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    public const string MaterializerName = "Json";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <summary>
    /// Constructor.
    /// </summary>
    public JsonWorkflowMaterializer(SerializerOptionsProvider serializerOptionsProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    /// <inheritdoc />
    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var workflow = ToWorkflow(definition);
        return new ValueTask<Workflow>(workflow);
    }

    private Workflow ToWorkflow(WorkflowDefinition definition)
    {
        var root = JsonSerializer.Deserialize<IActivity>(definition.StringData!, _serializerOptionsProvider.CreateDefaultOptions())!;
        
        return new(
            new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id),
            new WorkflowPublication(definition.IsLatest, definition.IsPublished),
            new WorkflowMetadata(definition.Name, definition.Description, definition.CreatedAt),
            definition.Options,
            root,
            definition.Variables,
            definition.CustomProperties);
    }
}