using System.Text.Json;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Materializers;

public class JsonWorkflowMaterializer : IWorkflowMaterializer
{
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;
    public const string MaterializerName = "Json";
    public string Name => MaterializerName;

    public JsonWorkflowMaterializer(WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var root = JsonSerializer.Deserialize<IActivity>(definition.StringData!, _workflowSerializerOptionsProvider.CreateDefaultOptions())!;
        var workflow = definition.ToWorkflow(root);
        return new ValueTask<Workflow>(workflow);
    }
}