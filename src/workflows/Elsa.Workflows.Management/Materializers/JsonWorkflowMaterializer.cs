using System.Text.Json;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Extensions;

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