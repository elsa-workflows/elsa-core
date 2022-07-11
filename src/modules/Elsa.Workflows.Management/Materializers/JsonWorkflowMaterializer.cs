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
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    public const string MaterializerName = "Json";
    public string Name => MaterializerName;

    public JsonWorkflowMaterializer(SerializerOptionsProvider serializerOptionsProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var root = JsonSerializer.Deserialize<IActivity>(definition.StringData!, _serializerOptionsProvider.CreateDefaultOptions())!;
        var workflow = definition.ToWorkflow(root);
        return new ValueTask<Workflow>(workflow);
    }
}