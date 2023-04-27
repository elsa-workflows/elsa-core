using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;

namespace Elsa.Workflows.Management.Materializers;

/// <summary>
/// Materializes a <see cref="Workflow"/> from a <see cref="WorkflowDefinition"/>'s JSON data.
/// </summary>
public class JsonWorkflowMaterializer : IWorkflowMaterializer
{
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "Json";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonWorkflowMaterializer"/> class.
    /// </summary>
    public JsonWorkflowMaterializer(WorkflowDefinitionMapper workflowDefinitionMapper)
    {
        _workflowDefinitionMapper = workflowDefinitionMapper;
    }

    /// <inheritdoc />
    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var workflow = ToWorkflow(definition);
        return new ValueTask<Workflow>(workflow);
    }

    private Workflow ToWorkflow(WorkflowDefinition definition) => _workflowDefinitionMapper.Map(definition);
}