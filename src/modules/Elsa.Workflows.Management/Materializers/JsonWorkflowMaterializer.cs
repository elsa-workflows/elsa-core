using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Materializers;

/// <summary>
/// Materializes a <see cref="Workflow"/> from a <see cref="WorkflowDefinition"/>'s JSON data.
/// </summary>
public class JsonWorkflowMaterializer : IWorkflowMaterializer
{
    private readonly IActivitySerializer _activitySerializer;

    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "Json";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonWorkflowMaterializer"/> class.
    /// </summary>
    public JsonWorkflowMaterializer(IActivitySerializer activitySerializer)
    {
        _activitySerializer = activitySerializer;
    }

    /// <inheritdoc />
    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var workflow = ToWorkflow(definition);
        return new ValueTask<Workflow>(workflow);
    }

    private Workflow ToWorkflow(WorkflowDefinition definition)
    {
        var root = _activitySerializer.Deserialize(definition.StringData!);
        
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